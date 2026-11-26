using Microsoft.CodeAnalysis;

namespace Tool.Analyzers;

internal enum LayoutStatus
{
    Known = 0,
    GenericOrUnresolved = 1,
    UnknownLayout = 2
}

internal readonly struct StructLayoutInfo(
    int size,
    int optimalSize,
    int alignment,
    bool hasReferences,
    LayoutStatus status,
    ITypeSymbol? unknownType = null)
{
    public static readonly StructLayoutInfo GenericOrUnresolved =
        new(0, 0, 1, hasReferences: false, LayoutStatus.GenericOrUnresolved);

    public readonly int Size = size;
    public readonly int OptimalSize = optimalSize;
    public readonly int Alignment = alignment;
    public readonly bool HasReferences = hasReferences;
    public readonly LayoutStatus Status = status;
    public readonly ITypeSymbol? UnknownType = unknownType;

    public bool IsKnown => Status == LayoutStatus.Known;
    public bool IsUnknownLayout => Status == LayoutStatus.UnknownLayout;

    public static StructLayoutInfo Known(int size, int optimalSize, int alignment, bool hasReferences) =>
        new(size, optimalSize, alignment, hasReferences, LayoutStatus.Known);

    public static StructLayoutInfo Unknown(ITypeSymbol unknownType) =>
        new(0, 0, 1, hasReferences: false, LayoutStatus.UnknownLayout, unknownType);
}
