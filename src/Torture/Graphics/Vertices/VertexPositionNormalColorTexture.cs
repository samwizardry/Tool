using System.Numerics;
using System.Runtime.InteropServices;

using Vortice.Mathematics;

namespace Torture.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionNormalColorTexture
{
    public static readonly VertexLayout Layout;

    public Vector3 Position;
    public Vector3 Normal;
    public Color4 Color;
    public Vector2 TexCoord;

    static VertexPositionNormalColorTexture()
    {
        Layout = new VertexLayout([
            new VertexElement(VertexElementSemantic.Position, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.Normal, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.Color, VertexElementFormat.Float4),
            new VertexElement(VertexElementSemantic.TexCoord, VertexElementFormat.Float2)
        ]);
    }

    public VertexPositionNormalColorTexture(Vector3 position, Vector3 normal, Color4 color, Vector2 texCoord)
    {
        Position = position;
        Normal = normal;
        Color = color;
        TexCoord = texCoord;
    }
}
