using System.Numerics;
using System.Runtime.InteropServices;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionTexture
{
    public static readonly VertexLayout Layout = new VertexLayout(
        VertexElement.Position,
        VertexElement.TexCoord);

    public Vector3 Position;
    public Vector2 TexCoord;

    public VertexPositionTexture(
        Vector3 position,
        Vector2 texCoord)
    {
        Position = position;
        TexCoord = texCoord;
    }
}
