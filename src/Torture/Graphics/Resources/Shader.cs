using Torture.Collections;

namespace Torture.Graphics;

public readonly record struct ShaderDescription
{
    public ShaderDescription(ShaderType shaderType,
        string entryPoint,
        bool packAsRowMajor = true,
        bool debug = false,
        string? debugName = null)
    {
        ShaderType = shaderType;
        EntryPoint = entryPoint;
        PackAsRowMajor = packAsRowMajor;
        Debug = debug;
        DebugName = debugName;
    }

    public readonly ShaderType ShaderType { get; init; }
    public readonly string EntryPoint { get; init; }
    public readonly bool PackAsRowMajor { get; init; }
    public readonly bool Debug { get; init; }
    public readonly string? DebugName { get; init; }

    public static ShaderDescription Vertex(
        string entryPoint = "VSMain",
        bool packAsRowMajor = true,
        bool debug = false,
        string? debugName = null) =>
        new(ShaderType.Vertex, entryPoint, packAsRowMajor, debug, debugName);

    public static ShaderDescription Pixel(
        string entryPoint = "PSMain",
        bool packAsRowMajor = true,
        bool debug = false,
        string? debugName = null) =>
        new(ShaderType.Pixel, entryPoint, packAsRowMajor, debug, debugName);

    public static ShaderDescription Compute(
        string entryPoint = "CSMain",
        bool packAsRowMajor = true,
        bool debug = false,
        string? debugName = null) =>
        new(ShaderType.Compute, entryPoint, packAsRowMajor, debug, debugName);
}

public readonly record struct ShaderHandle(SlotHandle Raw)
{
    public static ShaderHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
