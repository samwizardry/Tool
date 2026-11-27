using Microsoft.CodeAnalysis;

namespace Tool.Analyzers;

internal static class DiagnosticDescriptors
{
    private const string PerformanceCategory = "Performance";

    public static readonly DiagnosticDescriptor LargeStructParameter = new(
        id: DiagnosticIds.LargeStructParameter,
        title: "Large struct parameter should be passed by 'in'",
        messageFormat: "Parameter '{0}' of struct type '{1}' (~{2} bytes) should be passed using the 'in' modifier",
        category: PerformanceCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SmallStructInParameter = new(
        id: DiagnosticIds.SmallStructInParameter,
        title: "Small struct parameter should be passed by value",
        messageFormat: "Parameter '{0}' of small struct type '{1}' (~{2} bytes) should be passed by value instead of '{3}'",
        category: PerformanceCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StructPadding = new(
        id: DiagnosticIds.StructPadding,
        title: "Struct fields can be reordered to reduce padding",
        messageFormat: "Struct '{0}' is ~{1} bytes due to padding; reordering fields by alignment can reduce it to ~{2} bytes (saves {3} bytes)",
        category: PerformanceCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor LargeStructLocalCopy = new(
        id: DiagnosticIds.LargeStructLocalCopy,
        title: "Large struct is copied into a local variable",
        messageFormat: "Local '{0}' copies struct '{1}' (~{2} bytes) from a ref-addressable source; consider using 'ref readonly var'",
        category: PerformanceCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor LargeStructForEachCopy = new(
        id: DiagnosticIds.LargeStructForEachCopy,
        title: "Large struct is copied in foreach loop",
        messageFormat: "Loop variable '{0}' copies struct '{1}' (~{2} bytes) on each iteration; consider using '{3}'",
        category: PerformanceCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SmallStructRefReadOnlyLocal = new(
        id: DiagnosticIds.SmallStructRefReadOnlyLocal,
        title: "Small struct does not need 'ref' or 'ref readonly' local",
        messageFormat: "Local '{0}' of small struct type '{1}' (~{2} bytes) should be copied by value instead of '{3}'",
        category: PerformanceCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor LargeStructReturnByValue = new(
        id: DiagnosticIds.LargeStructReturnByValue,
        title: "Large struct can be returned by 'ref readonly'",
        messageFormat: "Member '{0}' returns struct '{1}' (~{2} bytes) by value from a ref-returnable source; consider returning 'ref readonly'",
        category: PerformanceCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SmallStructRefReadOnlyReturn = new(
        id: DiagnosticIds.SmallStructRefReadOnlyReturn,
        title: "Small struct should be returned by value",
        messageFormat: "Member '{0}' returns small struct '{1}' (~{2} bytes) by '{3}'; consider returning by value",
        category: PerformanceCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnknownStructLayout = new(
        id: DiagnosticIds.UnknownStructLayout,
        title: "Struct layout could not be determined",
        messageFormat: "Layout of struct '{0}' could not be determined (unknown type '{1}'); update StructLayoutCalculator if needed",
        category: PerformanceCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
