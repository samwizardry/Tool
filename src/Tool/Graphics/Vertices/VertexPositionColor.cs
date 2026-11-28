using System.Numerics;
using System.Runtime.InteropServices;

using Vortice.Mathematics;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionColor
{
    public static readonly VertexLayout Layout = new VertexLayout(
        VertexElement.Position,
        VertexElement.Color128);

    public Vector3 Position;
    public Color4 Color;

    public VertexPositionColor(
        Vector3 position,
        Color4 color)
    {
        Position = position;
        Color = color;
    }
}
