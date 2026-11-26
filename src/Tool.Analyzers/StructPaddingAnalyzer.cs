using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tool.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructPaddingAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.StructPadding,
        DiagnosticDescriptors.UnknownStructLayout
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var calculator = new StructLayoutCalculator(compilationContext.Compilation);
            compilationContext.RegisterSymbolAction(ctx => AnalyzeStruct(ctx, calculator), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeStruct(SymbolAnalysisContext context, StructLayoutCalculator calculator)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (type.TypeKind != TypeKind.Struct ||
            type.IsImplicitlyDeclared ||
            type.IsRefLikeType ||
            type.TypeParameters.Length > 0)
        {
            return;
        }

        // Respect explicit [StructLayout] (GPU constant buffers, interop, or LayoutKind.Auto) and [InlineArray].
        if (calculator.HasExplicitStructLayout(type) || calculator.GetInlineArrayLength(type) > 0)
        {
            return;
        }

        StructLayoutInfo layout = calculator.GetLayout(type);
        Location location = SymbolHelpers.GetPrimaryLocation(type);

        if (layout.IsUnknownLayout)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UnknownStructLayout,
                location,
                type.Name,
                (layout.UnknownType ?? type).ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return;
        }

        if (!layout.IsKnown || layout.HasReferences)
        {
            return;
        }

        if (layout.Size > layout.OptimalSize)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.StructPadding,
                location,
                type.Name,
                layout.Size,
                layout.OptimalSize,
                layout.Size - layout.OptimalSize));
        }
    }
}
