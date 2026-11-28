namespace Tool.Graphics;

public readonly record struct VertexLayout : IEquatable<VertexLayout>
{
    private readonly VertexElement[] _elements;
    public readonly uint ByteSize;

    public VertexLayout(params ReadOnlySpan<VertexElement> elements)
    {
        if (elements.Length < 1)
        {
            throw new InvalidOperationException();
        }

        _elements = new VertexElement[elements.Length];

        Span<uint> semanticCounter = stackalloc uint[(int)VertexElementSemantic.Count];
        uint offset = 0;

        for (int i = 0; i < elements.Length; i++)
        {
            var element = elements[i];

            if (element.SemanticName == VertexElementSemantic.Unknown ||
                element.SemanticName == VertexElementSemantic.Count ||
                element.Format == VertexElementFormat.Unknown)
            {
                throw new InvalidOperationException();
            }

            _elements[i] = new VertexElement(element.SemanticName, semanticCounter[(int)element.SemanticName]++, element.Format, offset);

            offset += element.Format.ByteSize();
        }

        ByteSize = offset;
    }

    public ReadOnlySpan<VertexElement> Elements => _elements.AsSpan();

    public bool Equals(VertexLayout other)
    {
        if (ByteSize != other.ByteSize || _elements.Length != other._elements.Length)
            return false;

        for (int i = 0; i < _elements.Length; i++)
        {
            if (_elements[i] != other._elements[i])
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(ByteSize);

        for (int i = 0; i < _elements.Length; i++)
        {
            hash.Add(_elements[i]);
        }

        return hash.ToHashCode();
    }
}
