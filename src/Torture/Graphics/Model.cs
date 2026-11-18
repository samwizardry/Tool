using System.Globalization;

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
public sealed class Mesh : IDisposable
{
    private readonly MeshPart[] _parts;
    private bool _disposed;

    public Mesh(Buffer vertexBuffer, Buffer? indexBuffer, MeshPart[]? parts = null)
    {
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;

        _parts = parts ??
            [
                indexBuffer is not null
                    ? MeshPart.Indexed(indexBuffer.Count)
                    : MeshPart.NonIndexed(vertexBuffer.Count)
            ];
    }

    public Buffer VertexBuffer { get; }

    public Buffer? IndexBuffer { get; }

    public ReadOnlySpan<MeshPart> Parts => _parts.AsSpan();

    public bool IsIndexed => IndexBuffer is not null;

    public void Dispose()
    {
        if (_disposed)
            return;

        VertexBuffer.Dispose();
        IndexBuffer?.Dispose();

        _disposed = true;
    }
}

public class Model : IDisposable
{
    private readonly Mesh[] _meshes;

    public Model(Mesh[] meshes)
    {
        _meshes = meshes;
    }

    public static Model FromLunaModel(GraphicsDevice graphicsDevice, Shader shader, string path)
    {
        using var fs = File.OpenRead(path);

        string vertexCountLine = "VertexCount:";
        string triangleCountLine = "TriangleCount:";
        string vertexListLine = "VertexList";
        string triangleListLine = "TriangleList";

        bool startReadVertices = false;
        bool startReadIndices = false;

        int vertexCount = 0;
        int triangleCount = 0;

        float[] vertices = null!;
        uint[] indices = null!;

        int startVertLoc = 0;
        int startIndLoc = 0;

        using var sr = new StreamReader(fs);

        while (!sr.EndOfStream)
        {
            var line = sr.ReadLine();
            if (line is null)
            {
                continue;
            }

            var lineAsSpan = line.AsSpan().Trim();

            // {
            if (lineAsSpan.Contains('{'))
            {
                continue;
            }

            // }
            if (lineAsSpan.Contains('}'))
            {
                startReadVertices = false;
                startReadIndices = false;
                continue;
            }

            // Vertices
            if (startReadVertices)
            {
                var s = lineAsSpan.Split(' ');
                foreach (var r in s)
                {
                    vertices[startVertLoc++] = float.Parse(lineAsSpan[r].Trim(), CultureInfo.InvariantCulture);
                }
            }

            // Indices
            if (startReadIndices)
            {
                var s = lineAsSpan.Split(' ');
                foreach (var r in s)
                {
                    indices[startIndLoc++] = uint.Parse(lineAsSpan[r], NumberStyles.Integer);
                }
            }

            // VertexList
            if (lineAsSpan.Contains(vertexListLine.AsSpan(), StringComparison.Ordinal))
            {
                startReadVertices = true;
                vertices = new float[vertexCount];
                continue;
            }

            // TriangleList
            if (lineAsSpan.Contains(triangleListLine.AsSpan(), StringComparison.Ordinal))
            {
                startReadIndices = true;
                indices = new uint[triangleCount * 3];
                continue;
            }

            // VertexCount
            int index = lineAsSpan.IndexOf(vertexCountLine);
            if (index != -1)
            {
                var s = lineAsSpan
                    .Slice(index + vertexCountLine.Length)
                    .Trim();

                vertexCount = int.Parse(s);
                continue;
            }

            // TriangleCount
            index = lineAsSpan.IndexOf(triangleCountLine);
            if (index != -1)
            {
                var s = lineAsSpan
                    .Slice(index + triangleCountLine.Length)
                    .Trim();

                triangleCount = int.Parse(s);
                continue;
            }
        }

        Mesh mesh = new(
            graphicsDevice.CreateBuffer(vertices, BufferDescription.Vertex(0, 0)),
            graphicsDevice.CreateBuffer(indices, BufferDescription.Index(0)));

        return new Model([mesh]);
    }

    public void Draw()
    {
    }

    public void Dispose()
    {
        for (int i = 0; i < _meshes.Length; i++)
        {
            _meshes[i].Dispose();
        }
    }
}
