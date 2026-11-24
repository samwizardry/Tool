using System.Numerics;
using System.Runtime.InteropServices;

using Vortice.Mathematics;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionColorTexture
{
    public static readonly VertexLayout Layout;

    public Vector3 Position;
    public Color4 Color;
    public Vector2 TexCoord;

    static VertexPositionColorTexture()
    {
        Layout = new VertexLayout([
            new VertexElement(VertexElementSemantic.Position, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.Color, VertexElementFormat.Float4),
            new VertexElement(VertexElementSemantic.TexCoord, VertexElementFormat.Float2)
        ]);
    }

    public VertexPositionColorTexture(Vector3 position, Color4 color, Vector2 texCoord)
    {
        Position = position;
        Color = color;
        TexCoord = texCoord;
    }
}
