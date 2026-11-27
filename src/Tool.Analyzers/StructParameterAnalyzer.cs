using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tool.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructParameterAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.LargeStructParameter,
        DiagnosticDescriptors.SmallStructInParameter,
        DiagnosticDescriptors.UnknownStructLayout
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var calculator = new StructLayoutCalculator(compilationContext.Compilation);
            compilationContext.RegisterSymbolAction(ctx => AnalyzeMethod(ctx, calculator), SymbolKind.Method);
        });
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, StructLayoutCalculator calculator)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.IsImplicitlyDeclared ||
            method.IsOverride ||
            method.IsAsync ||
            method.IsExtern)
        {
            return;
        }

        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.Constructor))
        {
            return;
        }

        if (SymbolHelpers.IsPositionalRecordPrimaryConstructor(method, context.CancellationToken))
        {
            return;
        }

        if (method.MethodKind == MethodKind.Ordinary && SymbolHelpers.IsInterfaceImplementation(method))
        {
            return;
        }

        foreach (IParameterSymbol param in method.Parameters)
        {
            if (param.IsParams ||
                param.RefKind == RefKind.Out ||
                SymbolHelpers.TryGetAnalyzableStruct(param.Type) is not { } structType)
            {
                continue;
            }

            Location location = SymbolHelpers.GetPrimaryLocation(param);
            StructLayoutInfo layout = calculator.GetLayout(structType);

            if (layout.IsUnknownLayout)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnknownStructLayout,
                    location,
                    structType.Name,
                    (layout.UnknownType ?? structType).ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                continue;
            }

            if (!layout.IsKnown || layout.Size <= 0)
            {
                continue;
            }

            if (layout.Size > SymbolHelpers.SizeThresholdBytes && param.RefKind == RefKind.None)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LargeStructParameter, location, param.Name, structType.Name, layout.Size));
            }
            else if (layout.Size < SymbolHelpers.SizeThresholdBytes &&
                     param.RefKind is RefKind.In or RefKind.Ref or RefKind.RefReadOnlyParameter)
            {
                string modifier = param.RefKind switch
                {
                    RefKind.Ref => "ref",
                    RefKind.RefReadOnlyParameter => "ref readonly",
                    _ => "in"
                };

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.SmallStructInParameter, location, param.Name, structType.Name, layout.Size, modifier));
            }
        }
    }
}
