using System.Numerics;
using System.Runtime.InteropServices;

using Vortice.Mathematics.PackedVector;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionNormalTangentColorTextureSkinning
{
    public static readonly VertexLayout Layout = new VertexLayout(
        VertexElement.Position,
        VertexElement.Normal,
        VertexElement.Tangent,
        VertexElement.Color32,
        VertexElement.TexCoord,
        VertexElement.BlendIndices,
        VertexElement.BlendWeight);

    public Vector3 Position;
    public Vector3 Normal;
    public Vector4 Tangent;
    public UByte4Normalized Color;
    public Vector2 TexCoord;
    public UByte4 BlendIndices;
    public UByte4Normalized BlendWeight;

    public VertexPositionNormalTangentColorTextureSkinning(
        Vector3 position,
        Vector3 normal,
        Vector4 tangent,
        UByte4Normalized color,
        Vector2 texCoord,
        UByte4 blendIndices,
        UByte4Normalized blendWeight)
    {
        Position = position;
        Normal = normal;
        Tangent = tangent;
        Color = color;
        TexCoord = texCoord;
        BlendIndices = blendIndices;
        BlendWeight = blendWeight;
    }
}
