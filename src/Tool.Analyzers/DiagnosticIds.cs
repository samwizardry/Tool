namespace Tool.Analyzers;

public static class DiagnosticIds
{
    public const string LargeStructParameter = "TOOL001";
    public const string SmallStructInParameter = "TOOL002";
    public const string StructPadding = "TOOL003";
    public const string LargeStructLocalCopy = "TOOL004";
    public const string LargeStructForEachCopy = "TOOL005";
    public const string SmallStructRefReadOnlyLocal = "TOOL006";
    public const string LargeStructReturnByValue = "TOOL007";
    public const string SmallStructRefReadOnlyReturn = "TOOL008";
    public const string UnknownStructLayout = "TOOL009";
}
