using System.Numerics;
using System.Runtime.InteropServices;

using Vortice.Mathematics;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionNormalColor
{
    public static readonly VertexLayout Layout = new VertexLayout(
        VertexElement.Position,
        VertexElement.Normal,
        VertexElement.Color128);

    public Vector3 Position;
    public Vector3 Normal;
    public Color4 Color;

    public VertexPositionNormalColor(
        Vector3 position,
        Vector3 normal,
        Color4 color)
    {
        Position = position;
        Normal = normal;
        Color = color;
    }
}
