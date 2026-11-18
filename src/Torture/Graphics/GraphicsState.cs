namespace Torture.Graphics;

/// <summary>
/// Represents a vertex buffer binding to input assembler slot.
/// </summary>
public readonly record struct VertexBufferBinding(
    Buffer Buffer,
    uint Slot = 0,
    uint ByteOffset = 0);

/// <summary>
/// Represents an index buffer binding to input assembler slot.
/// </summary>
public readonly record struct IndexBufferBinding(
    Buffer Buffer,
    uint ByteOffset = 0);

/// <summary>
/// Fixed-size zero-allocation container for up to 4 <see cref="BindingSet"/> slots (e.g. PerFrame, PerPass, PerMaterial, PerObject).
/// </summary>
public struct BindingSetVector
{
    public const int MaxBindingSets = 4;

    public BindingSet? Set0;
    public BindingSet? Set1;
    public BindingSet? Set2;
    public BindingSet? Set3;

    public readonly BindingSet? this[int index] => index switch
    {
        0 => Set0,
        1 => Set1,
        2 => Set2,
        3 => Set3,
        _ => throw new IndexOutOfRangeException()
    };
}

/// <summary>
/// Aggregates pipeline state, resource binding sets, and geometry buffers for draw calls.
/// </summary>
public readonly record struct GraphicsState(
    GraphicsPipeline Pipeline,
    BindingSetVector Bindings,
    VertexBufferBinding? VertexBuffer,
    IndexBufferBinding? IndexBuffer,
    Buffer? IndirectParams);
