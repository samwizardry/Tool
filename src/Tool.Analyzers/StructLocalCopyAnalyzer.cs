using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Tool.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructLocalCopyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.LargeStructLocalCopy,
        DiagnosticDescriptors.LargeStructForEachCopy,
        DiagnosticDescriptors.SmallStructRefReadOnlyLocal,
        DiagnosticDescriptors.UnknownStructLayout
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var calculator = new StructLayoutCalculator(compilationContext.Compilation);
            INamedTypeSymbol? spanType = compilationContext.Compilation.GetTypeByMetadataName("System.Span`1");
            INamedTypeSymbol? readOnlySpanType = compilationContext.Compilation.GetTypeByMetadataName("System.ReadOnlySpan`1");

            compilationContext.RegisterOperationBlockStartAction(blockContext =>
                AnalyzeOperationBlock(blockContext, calculator, spanType, readOnlySpanType));
        });
    }

    private static void AnalyzeOperationBlock(
        OperationBlockStartAnalysisContext blockContext,
        StructLayoutCalculator calculator,
        INamedTypeSymbol? spanType,
        INamedTypeSymbol? readOnlySpanType)
    {
        // 'ref' locals are illegal inside async methods.
        if (blockContext.OwningSymbol is IMethodSymbol { IsAsync: true })
        {
            return;
        }

        var candidates = new ConcurrentBag<LocalCopyCandidate>();
        var disqualified = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
        int hasYield = 0;

        blockContext.RegisterOperationAction(
            ctx => AnalyzeVariableDeclarator(ctx, calculator, candidates),
            OperationKind.VariableDeclarator);

        blockContext.RegisterOperationAction(
            ctx => AnalyzeForEachLoop(ctx, calculator, spanType, readOnlySpanType, candidates),
            OperationKind.Loop);

        blockContext.RegisterOperationAction(
            ctx => TrackLocalUsage((ILocalReferenceOperation)ctx.Operation, disqualified),
            OperationKind.LocalReference);

        blockContext.RegisterOperationAction(
            _ => Interlocked.Exchange(ref hasYield, 1),
            OperationKind.YieldReturn,
            OperationKind.YieldBreak);

        blockContext.RegisterOperationBlockEndAction(endContext =>
        {
            // 'ref' locals are illegal inside iterator methods (yield return / yield break).
            if (Volatile.Read(ref hasYield) != 0)
            {
                return;
            }

            foreach (LocalCopyCandidate candidate in candidates)
            {
                if (disqualified.ContainsKey(candidate.Local))
                {
                    continue;
                }

                if (candidate.UnknownType is not null)
                {
                    endContext.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.UnknownStructLayout,
                        candidate.Location,
                        candidate.StructType.Name,
                        candidate.UnknownType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                }
                else if (candidate.ForEachSuggestion is not null)
                {
                    endContext.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.LargeStructForEachCopy,
                        candidate.Location,
                        candidate.Local.Name,
                        candidate.StructType.Name,
                        candidate.Size,
                        candidate.ForEachSuggestion));
                }
                else
                {
                    endContext.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.LargeStructLocalCopy,
                        candidate.Location,
                        candidate.Local.Name,
                        candidate.StructType.Name,
                        candidate.Size));
                }
            }
        });
    }

    private static void AnalyzeVariableDeclarator(
        OperationAnalysisContext context,
        StructLayoutCalculator calculator,
        ConcurrentBag<LocalCopyCandidate> candidates)
    {
        var declarator = (IVariableDeclaratorOperation)context.Operation;
        ILocalSymbol local = declarator.Symbol;

        // Foreach loop variables have no initializer here and are analyzed separately.
        if (local.IsConst || declarator.Initializer is null)
        {
            return;
        }

        // 'using var' cannot be combined with 'ref readonly'.
        if (declarator.Parent?.Parent is IUsingDeclarationOperation)
        {
            return;
        }

        if (SymbolHelpers.TryGetAnalyzableStruct(local.Type) is not { } structType)
        {
            return;
        }

        bool isRefReadOnly = local.IsRef && local.RefKind == RefKind.In;
        bool isCopyCandidate = !local.IsRef &&
            !IsInsideAsyncFunction(declarator) &&
            IsRefAddressableSource(declarator.Initializer.Value, isRoot: true);

        if (!isRefReadOnly && !isCopyCandidate)
        {
            return;
        }

        Location location = SymbolHelpers.GetPrimaryLocation(local);
        StructLayoutInfo layout = calculator.GetLayout(structType);

        if (layout.IsUnknownLayout)
        {
            ITypeSymbol unknownType = layout.UnknownType ?? structType;
            if (isRefReadOnly)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnknownStructLayout,
                    location,
                    structType.Name,
                    unknownType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
            else
            {
                candidates.Add(new LocalCopyCandidate(local, structType, 0, location, forEachSuggestion: null, unknownType));
            }

            return;
        }

        if (!layout.IsKnown || layout.Size <= 0)
        {
            return;
        }

        if (isRefReadOnly && layout.Size < SymbolHelpers.SizeThresholdBytes)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SmallStructRefReadOnlyLocal,
                location,
                local.Name,
                structType.Name,
                layout.Size));
        }
        else if (isCopyCandidate && layout.Size > SymbolHelpers.SizeThresholdBytes)
        {
            candidates.Add(new LocalCopyCandidate(local, structType, layout.Size, location, forEachSuggestion: null, unknownType: null));
        }
    }

    private static void AnalyzeForEachLoop(
        OperationAnalysisContext context,
        StructLayoutCalculator calculator,
        INamedTypeSymbol? spanType,
        INamedTypeSymbol? readOnlySpanType,
        ConcurrentBag<LocalCopyCandidate> candidates)
    {
        if (context.Operation is not IForEachLoopOperation forEach ||
            forEach.LoopControlVariable is not IVariableDeclaratorOperation loopDeclarator)
        {
            return;
        }

        ILocalSymbol local = loopDeclarator.Symbol;
        if (SymbolHelpers.TryGetAnalyzableStruct(local.Type) is not { } structType)
        {
            return;
        }

        bool isRefReadOnly = local.IsRef && local.RefKind == RefKind.In;
        string? forEachSuggestion = null;

        if (!local.IsRef && !IsInsideAsyncFunction(forEach))
        {
            IOperation collection = UnwrapConversions(forEach.Collection);
            if (collection.Type is IArrayTypeSymbol { Rank: 1 })
            {
                forEachSuggestion = "ref readonly var ... in array.AsSpan()";
            }
            else if (collection.Type is not null &&
                     SupportsRefIteration(collection.Type, calculator, spanType, readOnlySpanType))
            {
                forEachSuggestion = "ref readonly var";
            }
        }

        if (!isRefReadOnly && forEachSuggestion is null)
        {
            return;
        }

        Location location = SymbolHelpers.GetPrimaryLocation(local);
        StructLayoutInfo layout = calculator.GetLayout(structType);

        if (layout.IsUnknownLayout)
        {
            ITypeSymbol unknownType = layout.UnknownType ?? structType;
            if (isRefReadOnly)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnknownStructLayout,
                    location,
                    structType.Name,
                    unknownType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
            else
            {
                candidates.Add(new LocalCopyCandidate(local, structType, 0, location, forEachSuggestion, unknownType));
            }

            return;
        }

        if (!layout.IsKnown || layout.Size <= 0)
        {
            return;
        }

        if (isRefReadOnly && layout.Size < SymbolHelpers.SizeThresholdBytes)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SmallStructRefReadOnlyLocal,
                location,
                local.Name,
                structType.Name,
                layout.Size));
        }
        else if (forEachSuggestion is not null && layout.Size > SymbolHelpers.SizeThresholdBytes)
        {
            candidates.Add(new LocalCopyCandidate(local, structType, layout.Size, location, forEachSuggestion, unknownType: null));
        }
    }

    private static bool SupportsRefIteration(
        ITypeSymbol collectionType,
        StructLayoutCalculator calculator,
        INamedTypeSymbol? spanType,
        INamedTypeSymbol? readOnlySpanType)
    {
        if (collectionType is not INamedTypeSymbol named)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, spanType) ||
            SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, readOnlySpanType))
        {
            return true;
        }

        if (calculator.GetInlineArrayLength(named) > 0)
        {
            return true;
        }

        // Check custom ref-returning GetEnumerator().Current patterns.
        foreach (ISymbol member in named.GetMembers("GetEnumerator"))
        {
            if (member is IMethodSymbol { Parameters.Length: 0, ReturnType: INamedTypeSymbol enumeratorType })
            {
                foreach (ISymbol enumMember in enumeratorType.GetMembers("Current"))
                {
                    if (enumMember is IPropertySymbol { ReturnsByRef: true } or IPropertySymbol { ReturnsByRefReadonly: true })
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static void TrackLocalUsage(
        ILocalReferenceOperation localRef,
        ConcurrentDictionary<ISymbol, byte> disqualified)
    {
        if (IsCapturedByNestedFunction(localRef) || IsMutatingOrIncompatibleUsage(localRef))
        {
            disqualified.TryAdd(localRef.Local, 0);
        }
    }

    private static bool IsRefAddressableSource(IOperation? operation, bool isRoot)
    {
        if (operation is null)
        {
            return false;
        }

        while (operation is IParenthesizedOperation paren)
        {
            operation = paren.Operand;
        }

        switch (operation)
        {
            case IInvocationOperation invocation:
                return invocation.TargetMethod.ReturnsByRef || invocation.TargetMethod.ReturnsByRefReadonly;

            case IPropertyReferenceOperation propRef:
                return propRef.Property.ReturnsByRef || propRef.Property.ReturnsByRefReadonly;

            case IArrayElementReferenceOperation:
                return true;

            case IInlineArrayAccessOperation inlineAccess:
                return inlineAccess.Type is { IsRefLikeType: false } &&
                       IsRefAddressableSource(inlineAccess.Instance, isRoot: false);

            case IFieldReferenceOperation fieldRef:
                if (fieldRef.Field.IsConst)
                {
                    return false;
                }

                if (fieldRef.Field.IsStatic || fieldRef.Field.ContainingType.IsReferenceType)
                {
                    return true;
                }

                return IsRefAddressableSource(fieldRef.Instance, isRoot: false);

            case IParameterReferenceOperation:
            case ILocalReferenceOperation:
            case IInstanceReferenceOperation:
                // Allow as receiver in a chain (e.g. 'state.Viewport'), but skip direct 'var copy = state;'.
                return !isRoot;

            default:
                return false;
        }
    }

    private static bool IsInsideAsyncFunction(IOperation operation)
    {
        for (IOperation? current = operation.Parent; current is not null; current = current.Parent)
        {
            if (current is IAnonymousFunctionOperation { Symbol.IsAsync: true } ||
                current is ILocalFunctionOperation { Symbol.IsAsync: true })
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCapturedByNestedFunction(ILocalReferenceOperation localRef)
    {
        ISymbol? declaringSymbol = localRef.Local.ContainingSymbol;

        for (IOperation? current = localRef.Parent; current is not null; current = current.Parent)
        {
            if (current is IAnonymousFunctionOperation anon)
            {
                return !SymbolEqualityComparer.Default.Equals(declaringSymbol, anon.Symbol);
            }

            if (current is ILocalFunctionOperation localFn)
            {
                return !SymbolEqualityComparer.Default.Equals(declaringSymbol, localFn.Symbol);
            }
        }

        return false;
    }

    private static bool IsMutatingOrIncompatibleUsage(ILocalReferenceOperation localRef)
    {
        if (localRef.Parent is not { } parent)
        {
            return false;
        }

        switch (parent)
        {
            case IAssignmentOperation assign when assign.Target == localRef:
            case IIncrementOrDecrementOperation:
            case IAddressOfOperation:
                return true;

            case IArgumentOperation arg when arg.Parameter?.RefKind is RefKind.Ref or RefKind.Out:
                return true;

            case IVariableInitializerOperation init
                when init.Parent is IVariableDeclaratorOperation { Symbol: { IsRef: true, RefKind: RefKind.Ref } }:
                return true;

            case IInvocationOperation inv when inv.Instance == localRef:
                return !inv.TargetMethod.IsReadOnly && !localRef.Local.Type.IsReadOnly;

            case IFieldReferenceOperation fieldRef when fieldRef.Instance == localRef:
                return IsWriteTarget(fieldRef);

            case IPropertyReferenceOperation propRef when propRef.Instance == localRef:
                if (IsWriteTarget(propRef))
                {
                    return true;
                }
                return propRef.Property.GetMethod is { IsReadOnly: false } && !localRef.Local.Type.IsReadOnly;

            case IInlineArrayAccessOperation inlineAccess when inlineAccess.Instance == localRef:
                return IsWriteTarget(inlineAccess);

            default:
                return false;
        }
    }

    private static bool IsWriteTarget(IOperation op)
    {
        IOperation? parent = op.Parent;
        while (parent is not null)
        {
            switch (parent)
            {
                case IAssignmentOperation assign when assign.Target == op:
                case IIncrementOrDecrementOperation:
                case IAddressOfOperation:
                    return true;

                case IArgumentOperation arg when arg.Parameter?.RefKind is RefKind.Ref or RefKind.Out:
                    return true;

                case IFieldReferenceOperation fieldRef when fieldRef.Instance == op:
                case IPropertyReferenceOperation propRef when propRef.Instance == op:
                case IInlineArrayAccessOperation inlineAccess when inlineAccess.Instance == op:
                    op = parent;
                    parent = op.Parent;
                    continue;

                default:
                    return false;
            }
        }

        return false;
    }

    private static IOperation UnwrapConversions(IOperation operation)
    {
        while (operation is IConversionOperation conv)
        {
            operation = conv.Operand;
        }

        return operation;
    }

    private readonly struct LocalCopyCandidate(
        ILocalSymbol local,
        INamedTypeSymbol structType,
        int size,
        Location location,
        string? forEachSuggestion,
        ITypeSymbol? unknownType)
    {
        public readonly ILocalSymbol Local = local;
        public readonly INamedTypeSymbol StructType = structType;
        public readonly int Size = size;
        public readonly Location Location = location;
        public readonly string? ForEachSuggestion = forEachSuggestion;
        public readonly ITypeSymbol? UnknownType = unknownType;
    }
}
