namespace Torture.Graphics;

/// <summary>
/// Represents a drawable sub-region of a <see cref="Mesh"/> associated with a material slot.
/// Supports both indexed and non-indexed geometry.
/// </summary>
public readonly record struct MeshPart(
    uint IndexCount,
    uint StartIndexLocation = 0,
    int BaseVertexLocation = 0,
    uint VertexCount = 0,
    uint StartVertexLocation = 0,
    int MaterialIndex = 0)
{
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
    private readonly MeshPart[] _parts;

    public Mesh(BufferHandle vertexBuffer, BufferHandle indexBuffer, MeshPart[] parts)
    {
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        _parts = parts;
    }

    public static Mesh Create(GraphicsDevice graphicsDevice, BufferHandle vertexBuffer, BufferHandle indexBuffer = default)
    {
        MeshPart part = indexBuffer.IsValid
            ? MeshPart.Indexed(graphicsDevice.GetBufferDescription(indexBuffer).Count)
            : MeshPart.NonIndexed(graphicsDevice.GetBufferDescription(vertexBuffer).Count);

        return new Mesh(vertexBuffer, indexBuffer, [part]);
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
