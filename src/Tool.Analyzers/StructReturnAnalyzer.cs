using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Tool.Analyzers;

/// <summary>
/// Audits struct return values on methods, properties, indexers, delegates, and local functions
/// against the 16-byte size threshold.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructReturnAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.LargeStructReturnByValue,
        DiagnosticDescriptors.SmallStructRefReturn,
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
                ctx => AnalyzeMethodSmallRefReturn(ctx, calculator),
                SymbolKind.Method);

            compilationContext.RegisterSymbolAction(
                ctx => AnalyzeDelegateSmallRefReturn(ctx, calculator),
                SymbolKind.NamedType);

            compilationContext.RegisterOperationAction(
                ctx => AnalyzeLocalFunctionReturn(ctx, calculator),
                OperationKind.LocalFunction);

            compilationContext.RegisterOperationBlockStartAction(
                blockContext => AnalyzeLargeReturnByValue(blockContext, calculator));
        });
    }

    private static void AnalyzeMethodSmallRefReturn(SymbolAnalysisContext context, StructLayoutCalculator calculator)
    {
        var method = (IMethodSymbol)context.Symbol;

        if ((!method.ReturnsByRef && !method.ReturnsByRefReadonly) ||
            method.IsImplicitlyDeclared ||
            method.IsOverride ||
            method.IsExtern ||
            method.PartialDefinitionPart is not null)
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

        ISymbol target = method.AssociatedSymbol ?? method;
        ReportIfSmallRefReturn(method, target, calculator, context.ReportDiagnostic);
    }

    private static void AnalyzeDelegateSmallRefReturn(SymbolAnalysisContext context, StructLayoutCalculator calculator)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.IsImplicitlyDeclared ||
            type.TypeKind != TypeKind.Delegate ||
            type.DelegateInvokeMethod is not { } invoke)
        {
            return;
        }

        if (!invoke.ReturnsByRef && !invoke.ReturnsByRefReadonly)
        {
            return;
        }

        ReportIfSmallRefReturn(invoke, type, calculator, context.ReportDiagnostic);
    }

    private static void AnalyzeLocalFunctionReturn(OperationAnalysisContext context, StructLayoutCalculator calculator)
    {
        var localFunc = (ILocalFunctionOperation)context.Operation;
        IMethodSymbol method = localFunc.Symbol;

        if (method.IsImplicitlyDeclared || method.IsExtern)
        {
            return;
        }

        if (method.ReturnsByRef || method.ReturnsByRefReadonly)
        {
            ReportIfSmallRefReturn(method, method, calculator, context.ReportDiagnostic);
            return;
        }

        if (method.IsAsync || localFunc.Body is null)
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

        if (!AllDirectReturnsAreRefReturnable(localFunc.Body))
        {
            return;
        }

        Location location = SymbolHelpers.GetPrimaryLocation(method);
        if (layout.IsUnknownLayout)
        {
            context.ReportDiagnostic(SymbolHelpers.CreateUnknownLayoutDiagnostic(location, structType, layout));
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.LargeStructReturnByValue,
            location,
            method.Name,
            SymbolHelpers.FormatTypeName(structType),
            layout.Size));
    }

    private static void ReportIfSmallRefReturn(
        IMethodSymbol method,
        ISymbol target,
        StructLayoutCalculator calculator,
        Action<Diagnostic> reportDiagnostic)
    {
        if (SymbolHelpers.TryGetAnalyzableStruct(method.ReturnType) is not { } structType)
        {
            return;
        }

        Location location = SymbolHelpers.GetPrimaryLocation(target);
        StructLayoutInfo layout = calculator.GetLayout(structType);

        if (layout.IsUnknownLayout)
        {
            reportDiagnostic(SymbolHelpers.CreateUnknownLayoutDiagnostic(location, structType, layout));
            return;
        }

        if (!layout.IsKnown || layout.Size <= 0 || layout.Size >= SymbolHelpers.SizeThresholdBytes)
        {
            return;
        }

        reportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.SmallStructRefReturn,
            location,
            target.Name,
            SymbolHelpers.FormatTypeName(structType),
            layout.Size,
            SymbolHelpers.FormatLocalOrReturnRefKind(method.RefKind)));
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

        blockContext.RegisterOperationAction(ctx =>
        {
            if (!IsInsideNestedFunction(ctx.Operation))
            {
                Interlocked.Exchange(ref hasYield, 1);
            }
        }, OperationKind.YieldReturn, OperationKind.YieldBreak);

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
                endContext.ReportDiagnostic(SymbolHelpers.CreateUnknownLayoutDiagnostic(location, structType, layout));
                return;
            }

            endContext.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LargeStructReturnByValue,
                location,
                target.Name,
                SymbolHelpers.FormatTypeName(structType),
                layout.Size));
        });
    }

    private static bool AllDirectReturnsAreRefReturnable(IOperation root)
    {
        int returnCount = 0;
        var stack = new Stack<IOperation>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            IOperation current = stack.Pop();

            if (current.Kind is OperationKind.YieldReturn or OperationKind.YieldBreak)
            {
                return false;
            }

            if (current is IReturnOperation returnOp)
            {
                if (returnOp.ReturnedValue is null)
                {
                    continue;
                }

                returnCount++;
                if (!IsRefReturnable(returnOp.ReturnedValue))
                {
                    return false;
                }

                continue;
            }

            foreach (IOperation child in current.ChildOperations)
            {
                if (child is IAnonymousFunctionOperation or ILocalFunctionOperation)
                {
                    continue;
                }

                stack.Push(child);
            }
        }

        return returnCount > 0;
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

                return IsRefReturnable(fieldRef.Instance);

            case IArrayElementReferenceOperation:
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
                return false;
        }
    }
}
