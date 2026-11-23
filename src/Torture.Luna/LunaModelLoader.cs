using System.Globalization;

using Torture.Assets;
using Torture.Graphics;

namespace Torture.Luna;

public sealed class LunaModelLoader : IModelLoader
{
    private Mesh Import(GraphicsDevice device, string path)
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
            if (line == null)
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
                    var token = lineAsSpan[r].Trim();
                    if (token.Length == 0)
                        continue;

                    vertices[startVertLoc++] = float.Parse(token, CultureInfo.InvariantCulture);
                }

                // add TexCoord
                vertices[startVertLoc++] = 0.0f;
                vertices[startVertLoc++] = 0.0f;
            }

            // Indices
            if (startReadIndices)
            {
                var s = lineAsSpan.Split(' ');
                foreach (var r in s)
                {
                    var token = lineAsSpan[r].Trim();
                    if (token.Length == 0)
                        continue;

                    indices[startIndLoc++] = uint.Parse(token, NumberStyles.Integer);
                }
            }

            // VertexList
            if (lineAsSpan.Contains(vertexListLine.AsSpan(), StringComparison.Ordinal))
            {
                startReadVertices = true;
                vertices = new float[vertexCount * 8];
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

        Mesh mesh = Mesh.Create(device,
            device.CreateBuffer(vertices, BufferDescription.Vertex(0, 8 * sizeof(float))),
            device.CreateBuffer(indices, BufferDescription.Index(0)));

        return mesh;
    }

    public ModelAsset Load(GraphicsDevice device, AssetManager assets, string virtualPath)
    {
        string fullPath = assets.ResolvePath(virtualPath);
        Mesh? mesh = null;
        try
        {
            mesh = Import(device, fullPath);
            assets.AddRef(ResourceFactoryCache.CarMaterial);
            return new ModelAsset
            {
                Meshes = [mesh],
                Materials = [ResourceFactoryCache.CarMaterial]
            };
        }
        catch
        {
            mesh?.Destroy(device);
            throw;
        }
    }
}
