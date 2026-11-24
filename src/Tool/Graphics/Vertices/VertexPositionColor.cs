using System.Numerics;
using System.Runtime.InteropServices;

using Vortice.Mathematics;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionColor
{
    public static readonly VertexLayout Layout;

    public Vector3 Position;
    public Color4 Color;

    static VertexPositionColor()
    {
        Layout = new VertexLayout([
            new VertexElement(VertexElementSemantic.Position, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.Color, VertexElementFormat.Float4),
        ]);
    }

    public VertexPositionColor(Vector3 position, Color4 color)
    {
        Position = position;
        Color = color;
    }
}
