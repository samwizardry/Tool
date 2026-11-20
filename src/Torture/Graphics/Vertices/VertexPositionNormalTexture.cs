using System.Numerics;
using System.Runtime.InteropServices;

namespace Torture.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct VertexPositionNormalTexture
{
    public static readonly VertexLayout Layout;

    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TexCoord;

    public VertexPositionNormalTexture(Vector3 position, Vector3 normal, Vector2 texCoord)
    {
        Position = position;
        Normal = normal;
        TexCoord = texCoord;
    }

    static VertexPositionNormalTexture()
    {
        Layout = new VertexLayout([
            new VertexElement(VertexElementSemantic.Position, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.Normal, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.TexCoord, VertexElementFormat.Float2)
        ]);
    }
}
