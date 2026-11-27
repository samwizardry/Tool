namespace Tool.Graphics;

/// <summary>
/// Represents a drawable sub-region of a <see cref="Mesh"/> associated with a material slot.
/// Supports both indexed and non-indexed geometry.
/// </summary>
public readonly record struct MeshPart
{
    public readonly uint IndexCount;
    public readonly uint StartIndexLocation;
    public readonly int BaseVertexLocation;
    public readonly uint VertexCount;
    public readonly uint StartVertexLocation;
    public readonly int MaterialIndex;

    public MeshPart(
        uint indexCount,
        uint startIndexLocation = 0,
        int baseVertexLocation = 0,
        uint vertexCount = 0,
        uint startVertexLocation = 0,
        int materialIndex = 0)
    {
        IndexCount = indexCount;
        StartIndexLocation = startIndexLocation;
        BaseVertexLocation = baseVertexLocation;
        VertexCount = vertexCount;
        StartVertexLocation = startVertexLocation;
        MaterialIndex = materialIndex;
    }

    public static MeshPart Indexed(uint indexCount, uint startIndex = 0, int baseVertex = 0, int materialIndex = 0) =>
        new(indexCount, startIndex, baseVertex, 0, 0, materialIndex);

    public static MeshPart NonIndexed(uint vertexCount, uint startVertex = 0, int materialIndex = 0) =>
        new(0, 0, 0, vertexCount, startVertex, materialIndex);
}

/// <summary>
/// Holds GPU geometry buffers (VertexBuffer and optional IndexBuffer) and sub-meshes (<see cref="MeshPart"/>).
/// </summary>
public sealed class Mesh
{
    public readonly IndexFormat IndexFormat;

    private readonly MeshPart[] _parts;

    public Mesh(BufferHandle vertexBuffer, BufferHandle indexBuffer, IndexFormat format, MeshPart[] parts)
    {
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        IndexFormat = format;
        _parts = parts;
    }

    public static Mesh Create(GraphicsDevice graphicsDevice, BufferHandle vertexBuffer, BufferHandle indexBuffer, IndexFormat format)
    {
        MeshPart part = indexBuffer.IsValid
            ? MeshPart.Indexed(graphicsDevice.GetBufferDescription(indexBuffer).Count)
            : MeshPart.NonIndexed(graphicsDevice.GetBufferDescription(vertexBuffer).Count);

        return new Mesh(vertexBuffer, indexBuffer, format, [part]);
    }

    public BufferHandle VertexBuffer { get; private set; }

    public BufferHandle IndexBuffer { get; private set; }

    public ReadOnlySpan<MeshPart> Parts => _parts.AsSpan();

    public bool IsIndexed => IndexBuffer.IsValid;

    public void Destroy(GraphicsDevice device)
    {
        if (VertexBuffer.IsValid)
        {
            device.Destroy(VertexBuffer);
            VertexBuffer = BufferHandle.Invalid;
        }

        if (IndexBuffer.IsValid)
        {
            device.Destroy(IndexBuffer);
            IndexBuffer = BufferHandle.Invalid;
        }
    }
}
