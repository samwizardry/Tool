using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Tool.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructReturnAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.LargeStructReturnByValue,
        DiagnosticDescriptors.SmallStructRefReadOnlyReturn,
        DiagnosticDescriptors.UnknownStructLayout
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var calculator = new StructLayoutCalculator(compilationContext.Compilation);

            compilationContext.RegisterSymbolAction(
                ctx => AnalyzeSmallRefReadOnlyReturn(ctx, calculator),
                SymbolKind.Method);

            compilationContext.RegisterOperationBlockStartAction(
                blockContext => AnalyzeLargeReturnByValue(blockContext, calculator));
        });
    }

    private static void AnalyzeSmallRefReadOnlyReturn(SymbolAnalysisContext context, StructLayoutCalculator calculator)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (!method.ReturnsByRefReadonly ||
            method.IsImplicitlyDeclared ||
            method.IsOverride ||
            method.IsExtern)
        {
            return;
        }

        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.PropertyGet))
        {
            return;
        }

        if (SymbolHelpers.IsInterfaceImplementation(method))
        {
            return;
        }

        if (SymbolHelpers.TryGetAnalyzableStruct(method.ReturnType) is not { } structType)
        {
            return;
        }

        ISymbol target = method.AssociatedSymbol ?? method;
        Location location = SymbolHelpers.GetPrimaryLocation(target);
        StructLayoutInfo layout = calculator.GetLayout(structType);

        if (layout.IsUnknownLayout)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UnknownStructLayout,
                location,
                structType.Name,
                (layout.UnknownType ?? structType).ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return;
        }

        if (!layout.IsKnown || layout.Size <= 0 || layout.Size >= SymbolHelpers.SizeThresholdBytes)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.SmallStructRefReadOnlyReturn,
            location,
            target.Name,
            structType.Name,
            layout.Size));
    }

    private static void AnalyzeLargeReturnByValue(
        OperationBlockStartAnalysisContext blockContext,
        StructLayoutCalculator calculator)
    {
        if (blockContext.OwningSymbol is not IMethodSymbol method)
        {
            return;
        }

        if (method.ReturnsByRef ||
            method.ReturnsByRefReadonly ||
            method.IsImplicitlyDeclared ||
            method.IsOverride ||
            method.IsVirtual ||
            method.IsAbstract ||
            method.IsAsync ||
            method.IsExtern)
        {
            return;
        }

        if (method.MethodKind == MethodKind.PropertyGet)
        {
            // C# forbids ref/ref readonly properties that also declare a setter or init accessor.
            if (method.AssociatedSymbol is not IPropertySymbol { SetMethod: null })
            {
                return;
            }
        }
        else if (method.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        if (SymbolHelpers.IsInterfaceImplementation(method))
        {
            return;
        }

        if (SymbolHelpers.TryGetAnalyzableStruct(method.ReturnType) is not { } structType)
        {
            return;
        }

        StructLayoutInfo layout = calculator.GetLayout(structType);
        if (layout.Status == LayoutStatus.GenericOrUnresolved ||
            (layout.IsKnown && layout.Size <= SymbolHelpers.SizeThresholdBytes))
        {
            return;
        }

        int returnCount = 0;
        int hasNonRefReturn = 0;
        int hasYield = 0;

        blockContext.RegisterOperationAction(ctx =>
        {
            var returnOp = (IReturnOperation)ctx.Operation;
            if (returnOp.ReturnedValue is null || IsInsideNestedFunction(returnOp))
            {
                return;
            }

            Interlocked.Increment(ref returnCount);

            if (!IsRefReturnable(returnOp.ReturnedValue))
            {
                Interlocked.Exchange(ref hasNonRefReturn, 1);
            }
        }, OperationKind.Return);

        blockContext.RegisterOperationAction(
            _ => Interlocked.Exchange(ref hasYield, 1),
            OperationKind.YieldReturn,
            OperationKind.YieldBreak);

        blockContext.RegisterOperationBlockEndAction(endContext =>
        {
            if (Volatile.Read(ref hasYield) != 0 ||
                Volatile.Read(ref returnCount) == 0 ||
                Volatile.Read(ref hasNonRefReturn) != 0)
            {
                return;
            }

            ISymbol target = method.AssociatedSymbol ?? method;
            Location location = SymbolHelpers.GetPrimaryLocation(target);

            if (layout.IsUnknownLayout)
            {
                endContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnknownStructLayout,
                    location,
                    structType.Name,
                    (layout.UnknownType ?? structType).ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                return;
            }

            endContext.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LargeStructReturnByValue,
                location,
                target.Name,
                structType.Name,
                layout.Size));
        });
    }

    private static bool IsInsideNestedFunction(IOperation operation)
    {
        for (IOperation? current = operation.Parent; current is not null; current = current.Parent)
        {
            if (current is IAnonymousFunctionOperation or ILocalFunctionOperation)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether an expression refers to storage that can safely escape the current method via 'return ref'.
    /// </summary>
    private static bool IsRefReturnable(IOperation? operation)
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
            case IFieldReferenceOperation fieldRef:
                if (fieldRef.Field.IsConst)
                {
                    return false;
                }

                if (fieldRef.Field.IsStatic || fieldRef.Field.ContainingType.IsReferenceType)
                {
                    return true;
                }

                // Struct fields cannot escape via 'this' (CS8170); they can only escape if the receiver escapes.
                return IsRefReturnable(fieldRef.Instance);

            case IArrayElementReferenceOperation:
                // Managed arrays live on the heap, so their elements can always escape by ref.
                return true;

            case IInlineArrayAccessOperation inlineAccess:
                return inlineAccess.Type is { IsRefLikeType: false } &&
                       IsRefReturnable(inlineAccess.Instance);

            case IParameterReferenceOperation paramRef:
                return paramRef.Parameter.RefKind is RefKind.Ref or RefKind.In or RefKind.RefReadOnlyParameter &&
                       paramRef.Parameter.ScopedKind == ScopedKind.None;

            case IPropertyReferenceOperation propRef:
                if (!propRef.Property.ReturnsByRef && !propRef.Property.ReturnsByRefReadonly)
                {
                    return false;
                }

                if (propRef.Property.IsStatic || propRef.Property.ContainingType.IsReferenceType)
                {
                    return true;
                }

                return IsRefReturnable(propRef.Instance);

            case IInvocationOperation invocation:
                if (!invocation.TargetMethod.ReturnsByRef && !invocation.TargetMethod.ReturnsByRefReadonly)
                {
                    return false;
                }

                if (!invocation.TargetMethod.IsStatic &&
                    !invocation.TargetMethod.ContainingType.IsReferenceType &&
                    !IsRefReturnable(invocation.Instance))
                {
                    return false;
                }

                // Conservatively ensure no ref/in/ref-struct argument references a local variable that cannot escape.
                foreach (IArgumentOperation arg in invocation.Arguments)
                {
                    bool mayContributeToRefReturn =
                        arg.Parameter is { RefKind: RefKind.Ref or RefKind.In or RefKind.RefReadOnlyParameter } ||
                        arg.Value.Type is { IsRefLikeType: true };

                    if (mayContributeToRefReturn && !IsRefReturnable(arg.Value))
                    {
                        return false;
                    }
                }

                return true;

            case IConditionalOperation conditional:
                return IsRefReturnable(conditional.WhenTrue) && IsRefReturnable(conditional.WhenFalse);

            default:
                // Locals, struct 'this', object creations, literals, and switch expressions cannot safely return by ref.
                return false;
        }
    }
}
