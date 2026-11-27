using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tool.Analyzers;

/// <summary>
/// Reports sequential unmanaged structs whose field order introduces avoidable alignment padding.
/// </summary>
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

        // Generic structs (including structs nested inside generic types) use Auto layout in CoreCLR.
        if (type.TypeKind != TypeKind.Struct ||
            type.IsImplicitlyDeclared ||
            type.IsRefLikeType ||
            type.IsGenericType)
        {
            return;
        }

        // Respect explicit [StructLayout] / [ExtendedLayout] / [FieldOffset] and [InlineArray].
        if (calculator.HasExplicitSourceLayout(type) || calculator.GetInlineArrayLength(type) > 0)
        {
            return;
        }

        StructLayoutInfo layout = calculator.GetLayout(type);
        Location location = SymbolHelpers.GetPrimaryLocation(type);

        if (layout.IsUnknownLayout)
        {
            context.ReportDiagnostic(SymbolHelpers.CreateUnknownLayoutDiagnostic(location, type, layout));
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
                SymbolHelpers.FormatTypeName(type),
                layout.Size,
                layout.OptimalSize,
                layout.Size - layout.OptimalSize));
        }
    }
}
