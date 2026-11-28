using System.Numerics;
using System.Runtime.InteropServices;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionNormalTexture
{
    public static readonly VertexLayout Layout = new VertexLayout(
        VertexElement.Position,
        VertexElement.Normal,
        VertexElement.TexCoord);

    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TexCoord;

    public VertexPositionNormalTexture(
        Vector3 position,
        Vector3 normal,
        Vector2 texCoord)
    {
        Position = position;
        Normal = normal;
        TexCoord = texCoord;
    }
}
