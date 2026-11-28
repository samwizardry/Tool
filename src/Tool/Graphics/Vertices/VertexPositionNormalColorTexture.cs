using System.Numerics;
using System.Runtime.InteropServices;

using Vortice.Mathematics;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionNormalColorTexture
{
    public static readonly VertexLayout Layout = new VertexLayout(
        VertexElement.Position,
        VertexElement.Normal,
        VertexElement.Color128,
        VertexElement.TexCoord);

    public Vector3 Position;
    public Vector3 Normal;
    public Color4 Color;
    public Vector2 TexCoord;

    public VertexPositionNormalColorTexture(
        Vector3 position,
        Vector3 normal,
        Color4 color,
        Vector2 texCoord)
    {
        Position = position;
        Normal = normal;
        Color = color;
        TexCoord = texCoord;
    }
}
