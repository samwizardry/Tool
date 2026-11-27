namespace Tool.Analyzers;

/// <summary>
/// Diagnostic IDs emitted by <c>Tool.Analyzers</c>.
/// </summary>
public static class DiagnosticIds
{
    public const string LargeStructParameter = "TOOL001";
    public const string SmallStructRefParameter = "TOOL002";
    public const string StructPadding = "TOOL003";
    public const string LargeStructLocalCopy = "TOOL004";
    public const string LargeStructForEachCopy = "TOOL005";
    public const string SmallStructRefLocal = "TOOL006";
    public const string LargeStructReturnByValue = "TOOL007";
    public const string SmallStructRefReturn = "TOOL008";
    public const string UnknownStructLayout = "TOOL009";
}
