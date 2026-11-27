using Tool.Collections;

namespace Tool.Graphics;

public readonly record struct ShaderDescription
{
    public readonly ShaderType ShaderType;
    public readonly string EntryPoint;
    public readonly bool PackAsRowMajor;
    public readonly bool Debug;
    public readonly string? DebugName;

    public ShaderDescription(
        ShaderType shaderType,
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

public readonly record struct ShaderHandle
{
    public readonly SlotHandle Raw;

    public ShaderHandle(SlotHandle raw)
    {
        Raw = raw;
    }

    public static ShaderHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
