using System.Runtime.InteropServices;

namespace Tool.Graphics;

public readonly record struct VertexElement
{
    public readonly VertexElementSemantic SemanticName;
    public readonly uint SemanticIndex;
    public readonly VertexElementFormat Format;
    public readonly uint ByteOffset;

    public VertexElement(VertexElementSemantic semanticName, VertexElementFormat format)
    {
        SemanticName = semanticName;
        Format = format;
    }

    public VertexElement(VertexElementSemantic semanticName, uint semanticIndex, VertexElementFormat format, uint byteOffset)
    {
        SemanticName = semanticName;
        SemanticIndex = semanticIndex;
        Format = format;
        ByteOffset = byteOffset;
    }

    public static VertexElement Position => new VertexElement(VertexElementSemantic.Position, VertexElementFormat.Float3);
    public static VertexElement Color128 => new VertexElement(VertexElementSemantic.Color, VertexElementFormat.Float4);
    public static VertexElement Color32 => new VertexElement(VertexElementSemantic.Color, VertexElementFormat.UNormByte4);
    public static VertexElement TexCoord => new VertexElement(VertexElementSemantic.TexCoord, VertexElementFormat.Float2);
    public static VertexElement Normal => new VertexElement(VertexElementSemantic.Normal, VertexElementFormat.Float3);
    public static VertexElement Tangent => new VertexElement(VertexElementSemantic.Tangent, VertexElementFormat.Float4);
    public static VertexElement BlendIndices => new VertexElement(VertexElementSemantic.BlendIndices, VertexElementFormat.UByte4);
    public static VertexElement BlendWeight => new VertexElement(VertexElementSemantic.BlendWeight, VertexElementFormat.UNormByte4);
}
