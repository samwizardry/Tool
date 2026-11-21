using System.Numerics;
using System.Runtime.InteropServices;

namespace Torture.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionTexture
{
    public static readonly VertexLayout Layout;

    public Vector3 Position;
    public Vector2 TexCoord;

    static VertexPositionTexture()
    {
        Layout = new VertexLayout([
            new VertexElement(VertexElementSemantic.Position, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.TexCoord, VertexElementFormat.Float2)
        ]);
    }

    public VertexPositionTexture(Vector3 position, Vector2 texCoord)
    {
        Position = position;
        TexCoord = texCoord;
    }
}
