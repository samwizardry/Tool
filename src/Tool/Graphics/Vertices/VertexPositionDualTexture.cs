using System.Numerics;
using System.Runtime.InteropServices;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionDualTexture
{
    public static readonly VertexLayout Layout;

    public Vector3 Position;
    public Vector2 TexCoord0;
    public Vector2 TexCoord1;

    static VertexPositionDualTexture()
    {
        Layout = new VertexLayout(
            new VertexElement(VertexElementSemantic.Position, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.TexCoord, VertexElementFormat.Float2),
            new VertexElement(VertexElementSemantic.TexCoord, VertexElementFormat.Float2));
    }

    public VertexPositionDualTexture(Vector3 position, Vector2 texCoord0, Vector2 texCoord1)
    {
        Position = position;
        TexCoord0 = texCoord0;
        TexCoord1 = texCoord1;
    }
}
