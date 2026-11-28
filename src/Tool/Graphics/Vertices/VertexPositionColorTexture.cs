using System.Numerics;
using System.Runtime.InteropServices;

using Vortice.Mathematics;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionColorTexture
{
    public static readonly VertexLayout Layout = new VertexLayout(
        VertexElement.Position,
        VertexElement.Color128,
        VertexElement.TexCoord);

    public Vector3 Position;
    public Color4 Color;
    public Vector2 TexCoord;

    public VertexPositionColorTexture(
        Vector3 position,
        Color4 color,
        Vector2 texCoord)
    {
        Position = position;
        Color = color;
        TexCoord = texCoord;
    }
}
