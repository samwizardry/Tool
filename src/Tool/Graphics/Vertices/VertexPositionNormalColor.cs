using System.Numerics;
using System.Runtime.InteropServices;

using Vortice.Mathematics;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionNormalColor
{
    public static readonly VertexLayout Layout;

    public Vector3 Position;
    public Vector3 Normal;
    public Color4 Color;

    static VertexPositionNormalColor()
    {
        Layout = new VertexLayout([
            new VertexElement(VertexElementSemantic.Position, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.Normal, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.Color, VertexElementFormat.Float4)
        ]);
    }

    public VertexPositionNormalColor(Vector3 position, Vector3 normal, Color4 color)
    {
        Position = position;
        Normal = normal;
        Color = color;
    }
}
