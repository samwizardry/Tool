using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Tool.Analyzers;

/// <summary>
/// Audits struct parameter passing against the 16-byte size threshold across methods,
/// constructors, C# 14 extension blocks, delegates, and local functions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructParameterAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.LargeStructParameter,
        DiagnosticDescriptors.SmallStructRefParameter,
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
                ctx => AnalyzeMethod(ctx, calculator),
                SymbolKind.Method);

            compilationContext.RegisterSymbolAction(
                ctx => AnalyzeNamedType(ctx, calculator),
                SymbolKind.NamedType);

            compilationContext.RegisterOperationAction(
                ctx => AnalyzeLocalFunction(ctx, calculator),
                OperationKind.LocalFunction);
        });
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, StructLayoutCalculator calculator)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.IsImplicitlyDeclared ||
            method.IsOverride ||
            method.IsAsync ||
            method.IsExtern ||
            method.PartialDefinitionPart is not null)
        {
            return;
        }

        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.Constructor))
        {
            return;
        }

        if (method.MethodKind == MethodKind.Ordinary && SymbolHelpers.IsInterfaceImplementation(method))
        {
            return;
        }

        // Primary constructors (CS8908 / CS9109) and iterators (CS1623) disallow adding 'in',
        // but existing ref/in/ref readonly modifiers on small structs are still reported.
        bool allowSuggestIn =
            !SymbolHelpers.IsPrimaryConstructor(method, context.CancellationToken) &&
            !SymbolHelpers.IsIteratorMethod(method, context.CancellationToken);

        AnalyzeParameters(method.Parameters, allowSuggestIn, calculator, context.ReportDiagnostic);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, StructLayoutCalculator calculator)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        // C# 14 extension block receiver parameter: extension(MyStruct s) / extension(in MyStruct s).
        if (type.IsExtension && type.ExtensionParameter is { IsImplicitlyDeclared: false } extensionParam)
        {
            // Unnamed extension(MyStruct) blocks declare only static members and pass no receiver.
            bool allowSuggestIn = !string.IsNullOrEmpty(extensionParam.Name);
            AnalyzeParameter(extensionParam, allowSuggestIn, calculator, context.ReportDiagnostic);
            return;
        }

        if (type.IsImplicitlyDeclared)
        {
            return;
        }

        if (type.TypeKind == TypeKind.Delegate && type.DelegateInvokeMethod is { } invoke)
        {
            AnalyzeParameters(invoke.Parameters, allowSuggestIn: true, calculator, context.ReportDiagnostic);
        }
    }

    private static void AnalyzeLocalFunction(OperationAnalysisContext context, StructLayoutCalculator calculator)
    {
        var localFunc = (ILocalFunctionOperation)context.Operation;
        IMethodSymbol method = localFunc.Symbol;

        if (method.IsImplicitlyDeclared || method.IsAsync || method.IsExtern)
        {
            return;
        }

        bool allowSuggestIn = !SymbolHelpers.IsIteratorMethod(method, context.CancellationToken);
        AnalyzeParameters(method.Parameters, allowSuggestIn, calculator, context.ReportDiagnostic);
    }

    private static void AnalyzeParameters(
        ImmutableArray<IParameterSymbol> parameters,
        bool allowSuggestIn,
        StructLayoutCalculator calculator,
        Action<Diagnostic> reportDiagnostic)
    {
        foreach (IParameterSymbol param in parameters)
        {
            AnalyzeParameter(param, allowSuggestIn, calculator, reportDiagnostic);
        }
    }

    private static void AnalyzeParameter(
        IParameterSymbol param,
        bool allowSuggestIn,
        StructLayoutCalculator calculator,
        Action<Diagnostic> reportDiagnostic)
    {
        if (param.IsParams ||
            param.RefKind == RefKind.Out ||
            SymbolHelpers.TryGetAnalyzableStruct(param.Type) is not { } structType)
        {
            return;
        }

        Location location = SymbolHelpers.GetPrimaryLocation(param);
        StructLayoutInfo layout = calculator.GetLayout(structType);

        if (layout.IsUnknownLayout)
        {
            reportDiagnostic(SymbolHelpers.CreateUnknownLayoutDiagnostic(location, structType, layout));
            return;
        }

        if (!layout.IsKnown || layout.Size <= 0)
        {
            return;
        }

        if (allowSuggestIn && layout.Size > SymbolHelpers.SizeThresholdBytes && param.RefKind == RefKind.None)
        {
            reportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LargeStructParameter,
                location,
                param.Name,
                SymbolHelpers.FormatTypeName(structType),
                layout.Size));
        }
        else if (layout.Size < SymbolHelpers.SizeThresholdBytes &&
                 param.RefKind is RefKind.In or RefKind.Ref or RefKind.RefReadOnlyParameter)
        {
            reportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.SmallStructRefParameter,
                location,
                param.Name,
                SymbolHelpers.FormatTypeName(structType),
                layout.Size,
                SymbolHelpers.FormatParameterRefKind(param.RefKind)));
        }
    }
}
