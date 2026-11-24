using Tool.Collections;

namespace Tool.Graphics;

public readonly record struct ShaderDescription(
    ShaderType ShaderType,
    string EntryPoint,
    bool PackAsRowMajor = true,
    bool Debug = false,
    string? DebugName = null)
{
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
