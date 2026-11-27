namespace Tool.Graphics;

public readonly record struct VertexElement
{
    public readonly uint SemanticIndex;
    public readonly uint ByteOffset;
    public readonly VertexElementSemantic SemanticName;
    public readonly VertexElementFormat Format;

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
}
