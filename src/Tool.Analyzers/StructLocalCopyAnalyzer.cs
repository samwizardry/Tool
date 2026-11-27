using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Tool.Analyzers;

/// <summary>
/// Audits struct local variables and foreach loop variables against the 16-byte size threshold.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructLocalCopyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.LargeStructLocalCopy,
        DiagnosticDescriptors.LargeStructForEachCopy,
        DiagnosticDescriptors.SmallStructRefLocal,
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

            compilationContext.RegisterOperationAction(
                ctx => AnalyzeVariableDeclarator(ctx, calculator),
                OperationKind.VariableDeclarator);

            compilationContext.RegisterOperationAction(
                ctx => AnalyzeForEachLoop(ctx, calculator, spanType, readOnlySpanType),
                OperationKind.Loop);
        });
    }

    private static void AnalyzeVariableDeclarator(OperationAnalysisContext context, StructLayoutCalculator calculator)
    {
        var declarator = (IVariableDeclaratorOperation)context.Operation;
        ILocalSymbol local = declarator.Symbol;

        // Foreach loop variables have no initializer here and are analyzed separately.
        if (local.IsConst || declarator.Initializer is null)
        {
            return;
        }

        // 'using var' cannot be combined with 'ref' / 'ref readonly'.
        if (declarator.Parent?.Parent is IUsingDeclarationOperation)
        {
            return;
        }

        if (SymbolHelpers.TryGetAnalyzableStruct(local.Type) is not { } structType)
        {
            return;
        }

        bool isCopyCandidate = !local.IsRef &&
            !IsInsideAsyncMethod(context, declarator) &&
            IsRefAddressableSource(declarator.Initializer.Value, isRoot: true);

        if (!local.IsRef && !isCopyCandidate)
        {
            return;
        }

        ReportLocalDiagnostic(context, calculator, local, structType, isCopyCandidate, forEachSuggestion: null);
    }

    private static void AnalyzeForEachLoop(
        OperationAnalysisContext context,
        StructLayoutCalculator calculator,
        INamedTypeSymbol? spanType,
        INamedTypeSymbol? readOnlySpanType)
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

        string? forEachSuggestion = null;
        if (!local.IsRef && !forEach.IsAsynchronous && !IsInsideAsyncMethod(context, forEach))
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

        if (!local.IsRef && forEachSuggestion is null)
        {
            return;
        }

        ReportLocalDiagnostic(context, calculator, local, structType, isCopyCandidate: false, forEachSuggestion);
    }

    private static void ReportLocalDiagnostic(
        OperationAnalysisContext context,
        StructLayoutCalculator calculator,
        ILocalSymbol local,
        INamedTypeSymbol structType,
        bool isCopyCandidate,
        string? forEachSuggestion)
    {
        Location location = SymbolHelpers.GetPrimaryLocation(local);
        StructLayoutInfo layout = calculator.GetLayout(structType);

        if (layout.IsUnknownLayout)
        {
            context.ReportDiagnostic(SymbolHelpers.CreateUnknownLayoutDiagnostic(location, structType, layout));
            return;
        }

        if (!layout.IsKnown || layout.Size <= 0)
        {
            return;
        }

        if (local.IsRef && layout.Size < SymbolHelpers.SizeThresholdBytes)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SmallStructRefLocal,
                location,
                local.Name,
                SymbolHelpers.FormatTypeName(structType),
                layout.Size,
                SymbolHelpers.FormatLocalOrReturnRefKind(local.RefKind)));
        }
        else if (layout.Size > SymbolHelpers.SizeThresholdBytes)
        {
            if (forEachSuggestion is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LargeStructForEachCopy,
                    location,
                    local.Name,
                    SymbolHelpers.FormatTypeName(structType),
                    layout.Size,
                    forEachSuggestion));
            }
            else if (isCopyCandidate)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LargeStructLocalCopy,
                    location,
                    local.Name,
                    SymbolHelpers.FormatTypeName(structType),
                    layout.Size));
            }
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
                return !isRoot;

            default:
                return false;
        }
    }

    private static bool IsInsideAsyncMethod(OperationAnalysisContext context, IOperation operation)
    {
        if (context.ContainingSymbol is IMethodSymbol { IsAsync: true })
        {
            return true;
        }

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

    private static IOperation UnwrapConversions(IOperation operation)
    {
        while (operation is IConversionOperation conv)
        {
            operation = conv.Operand;
        }

        return operation;
    }
}
