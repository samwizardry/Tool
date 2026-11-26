namespace Tool.Graphics;

public record struct VertexElement : IEquatable<VertexElement>
{
    public uint SemanticIndex = 0;
    public uint ByteOffset = 0;
    public readonly VertexElementSemantic SemanticName;
    public readonly VertexElementFormat Format;

    public VertexElement(VertexElementSemantic semanticName, VertexElementFormat format)
    {
        SemanticName = semanticName;
        Format = format;
    }

    public readonly bool Equals(VertexElement other)
    {
        return SemanticName == other.SemanticName &&
            SemanticIndex == other.SemanticIndex &&
            Format == other.Format &&
            ByteOffset == other.ByteOffset;
    }

    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SemanticName);
        hash.Add(SemanticIndex);
        hash.Add(Format);
        hash.Add(ByteOffset);
        return hash.ToHashCode();
    }
}
