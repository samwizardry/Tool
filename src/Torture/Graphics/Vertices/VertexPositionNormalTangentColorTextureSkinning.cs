using System.Numerics;
using System.Runtime.InteropServices;

using Vortice.Mathematics.PackedVector;

namespace Torture.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionNormalTangentColorTextureSkinning
{
    public static readonly VertexLayout Layout;

    public Vector3 Position;
    public Vector3 Normal;
    public Vector4 Tangent;
    public UByte4Normalized Color;
    public Vector2 TexCoord;
    public UByte4 BlendIndices;
    public UByte4Normalized BlendWeight;

    static VertexPositionNormalTangentColorTextureSkinning()
    {
        Layout = new VertexLayout([
            new VertexElement(VertexElementSemantic.Position, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.Normal, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.Tangent, VertexElementFormat.Float4),
            new VertexElement(VertexElementSemantic.Color, VertexElementFormat.UNormByte4),
            new VertexElement(VertexElementSemantic.TexCoord, VertexElementFormat.Float2),
            new VertexElement(VertexElementSemantic.BlendIndices, VertexElementFormat.UByte4),
            new VertexElement(VertexElementSemantic.BlendWeight, VertexElementFormat.UNormByte4),
        ]);
    }

    public VertexPositionNormalTangentColorTextureSkinning(Vector3 position, Vector3 normal, Vector4 tangent, UByte4Normalized color, Vector2 texCoord, UByte4 blendIndices, UByte4Normalized blendWeight)
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
