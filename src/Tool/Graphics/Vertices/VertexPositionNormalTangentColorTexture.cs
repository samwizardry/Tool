using System.Numerics;
using System.Runtime.InteropServices;

using Vortice.Mathematics.PackedVector;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionNormalTangentColorTexture
{
    public static readonly VertexLayout Layout = new VertexLayout(
        VertexElement.Position,
        VertexElement.Normal,
        VertexElement.Tangent,
        VertexElement.Color32,
        VertexElement.TexCoord);

    public Vector3 Position;
    public Vector3 Normal;
    public Vector4 Tangent;
    public UByte4Normalized Color;
    public Vector2 TexCoord;

    public VertexPositionNormalTangentColorTexture(
        Vector3 position,
        Vector3 normal,
        Vector4 tangent,
        UByte4Normalized color,
        Vector2 texCoord)
    {
        Position = position;
        Normal = normal;
        Tangent = tangent;
        Color = color;
        TexCoord = texCoord;
    }
}
