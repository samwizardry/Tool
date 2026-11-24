namespace Tool.Graphics;

/// <summary>
/// Represents a vertex buffer binding to input assembler slot.
/// </summary>
public readonly record struct VertexBufferBinding(
    BufferHandle Buffer,
    uint Slot = 0,
    uint ByteOffset = 0);

/// <summary>
/// Represents an index buffer binding to input assembler slot.
/// </summary>
public readonly record struct IndexBufferBinding(
    BufferHandle Buffer,
    uint ByteOffset = 0);

/// <summary>
/// Fixed-size zero-allocation container for up to 4 <see cref="BindingSet"/> slots (e.g. PerFrame, PerPass, PerMaterial, PerObject).
/// </summary>
public struct BindingSetVector
{
    public const int MaxBindingSets = 5;

    /// <summary>
    /// Rare
    /// </summary>
    public BindingSetHandle Set0;
    /// <summary>
    /// PerFrame
    /// </summary>
    public BindingSetHandle Set1;
    /// <summary>
    /// PerPass
    /// </summary>
    public BindingSetHandle Set2;
    /// <summary>
    /// PerMaterial
    /// </summary>
    public BindingSetHandle Set3;
    /// <summary>
    /// PerObject
    /// </summary>
    public BindingSetHandle Set4;

    public BindingSetHandle this[int index]
    {
        readonly get => index switch
        {
            0 => Set0,
            1 => Set1,
            2 => Set2,
            3 => Set3,
            4 => Set4,
            _ => throw new IndexOutOfRangeException(),
        };
        set
        {
            switch (index)
            {
                case 0: Set0 = value; break;
                case 1: Set1 = value; break;
                case 2: Set2 = value; break;
                case 3: Set3 = value; break;
                case 4: Set4 = value; break;
                default: throw new IndexOutOfRangeException();
            }
        }
    }
}

public readonly record struct GraphicsState(
    GraphicsPipelineHandle Pipeline,
    BindingSetVector Bindings = default,
    VertexBufferBinding VertexBuffer = default,
    IndexBufferBinding IndexBuffer = default);
