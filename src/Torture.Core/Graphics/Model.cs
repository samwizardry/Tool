using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;

using Torture.Core.Collections;

namespace Torture.Core.Graphics;

public class Model<TVertex, TIndex> : IDisposable
    where TVertex : unmanaged
    where TIndex : unmanaged
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Shader _shader;
    private readonly List<ModelMesh<TVertex, TIndex>> _meshes = [];

    public Model(GraphicsDevice graphicsDevice, Shader shader, List<ModelMesh<TVertex, TIndex>> meshes)
    {
        _graphicsDevice = graphicsDevice;
        _shader = shader;
        _meshes = meshes;
    }

    public static Model<VertexPositionNormal, uint> FromLunaModel(GraphicsDevice graphicsDevice, Shader shader, string path)
    {
        using var fs = File.OpenRead(path);

        int vertexCount = 0;
        int triangleCount = 0;

        string vertexCountLine = "VertexCount:";
        string triangleCountLine = "TriangleCount:";
        string vertexListLine = "VertexList";
        string triangleListLine = "TriangleList";

        List<VertexPositionNormal> vertices = [];
        List<uint> indices = [];

        Span<float> vertexElements = stackalloc float[6];
        Span<uint> indexElements = stackalloc uint[3];

        using var sr = new StreamReader(fs);
        bool startReadVertices = false;
        bool startReadIndices = false;

        int index = -1;

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

                int i = 0;
                foreach (var r in s)
                {
                    vertexElements[i++] = float.Parse(lineAsSpan[r].Trim(), CultureInfo.InvariantCulture);
                }

                vertices.Add(new(
                    new(vertexElements[..3]),
                    new(vertexElements[3..])));
            }

            // Indices
            if (startReadIndices)
            {
                var s = lineAsSpan.Split(' ');

                int i = 0;
                foreach (var r in s)
                {
                    indexElements[i++] = uint.Parse(lineAsSpan[r], NumberStyles.Integer);
                }

                indices.AddRange(indexElements);
            }

            // VertexList
            if (lineAsSpan.Contains(vertexListLine.AsSpan(), StringComparison.Ordinal))
            {
                startReadVertices = true;
                continue;
            }

            // TriangleList
            if (lineAsSpan.Contains(triangleListLine.AsSpan(), StringComparison.Ordinal))
            {
                startReadIndices = true;
                continue;
            }

            // VertexCount
            index = lineAsSpan.IndexOf(vertexCountLine);
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

        List<ModelMesh<VertexPositionNormal, uint>> meshes =
            [new ModelMesh<VertexPositionNormal, uint>(graphicsDevice, shader, vertices, indices, in VertexPositionNormal.Layout)];

        if (vertices.Count != vertexCount || indices.Count % 3 != 0 || indices.Count / 3 != triangleCount)
        {
            throw new InvalidOperationException();
        }

        return new Model<VertexPositionNormal, uint>(graphicsDevice, shader, meshes);
    }

    public void Draw()
    {
        foreach (var mesh in _meshes)
        {
            mesh.Draw();
        }
    }

    public void Dispose()
    {
        foreach (var mesh in _meshes)
        {
            mesh.Dispose();
        }
    }
}

public class ModelMesh<TVertex, TIndex> : IDisposable
    where TVertex : unmanaged
    where TIndex : unmanaged
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PerObjectData
    {
        public Matrix4x4 Model;
    }

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Shader _shader;
    private readonly Buffer _vertexBuffer;
    private readonly Buffer _indexBuffer;
    private readonly InputLayout _layout;
    private readonly Buffer _perObject;

    private PerObjectData _perObjectData = default;

    public ModelMesh(GraphicsDevice graphicsDevice, Shader shader, List<TVertex> vertices, List<TIndex> indices, in VertexLayout layout)
    {
        _graphicsDevice = graphicsDevice;
        _shader = shader;
        _vertexBuffer = graphicsDevice.CreateBuffer(vertices.AsSpan(), BufferDescription.Vertex(0, 0));
        _indexBuffer = graphicsDevice.CreateBuffer(indices.AsSpan(), BufferDescription.Index(0));
        _layout = graphicsDevice.CreateInputLayout(in layout, shader);
        _perObject = graphicsDevice.CreateBuffer(BufferDescription.Constant<PerObjectData>());
    }

    public void Draw()
    {
        _perObjectData.Model = Matrix4x4.Identity;

        _graphicsDevice.UpdateBuffer(_perObject, ref _perObjectData);
        _graphicsDevice.SetConstantBuffer(_perObject, ShaderType.Vertex | ShaderType.Pixel, 1u);

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.SetIndexBuffer(_indexBuffer);
        _graphicsDevice.SetInputLayout(_layout);
        _graphicsDevice.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
        _graphicsDevice.DrawIndexed(_indexBuffer.Count);
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _layout?.Dispose();
    }
}
