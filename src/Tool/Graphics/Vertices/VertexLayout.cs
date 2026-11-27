using System.Runtime.CompilerServices;

namespace Tool.Graphics;

[InlineArray(Constants.MaxVertexAttributes)]
public struct VertexElementArray
{
    private VertexElement _element0;
}

public readonly record struct VertexLayout : IEquatable<VertexLayout>
{
    private readonly VertexElementArray _elements;
    public readonly uint ByteSize;
    public readonly byte Count;

    public VertexLayout(params ReadOnlySpan<VertexElement> elements)
    {
        if (elements.IsEmpty || elements.Length > Constants.MaxVertexAttributes)
        {
            throw new ArgumentOutOfRangeException(nameof(elements));
        }

        VertexElementArray buffer = default;
        Span<uint> semanticCounter = stackalloc uint[(int)VertexElementSemantic.Count];
        uint offset = 0;

        for (int i = 0; i < elements.Length; i++)
        {
            var element = elements[i];

            if (element.SemanticName <= VertexElementSemantic.Unknown ||
                element.SemanticName >= VertexElementSemantic.Count ||
                element.Format == VertexElementFormat.Unknown)
            {
                throw new InvalidOperationException();
            }

            buffer[i] = new VertexElement(element.SemanticName, semanticCounter[(int)element.SemanticName]++, element.Format, offset);

            offset += element.Format.ByteSize();
        }

        _elements = buffer;
        ByteSize = offset;
        Count = (byte)elements.Length;
    }

    public VertexElement this[int index] => _elements[index];

    public bool Equals(VertexLayout other)
    {
        if (Count != other.Count || ByteSize != other.ByteSize)
            return false;

        for (int i = 0; i < Count; i++)
        {
            if (_elements[i] != other._elements[i])
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Count);
        hash.Add(ByteSize);

        for (int i = 0; i < Count; i++)
            hash.Add(_elements[i]);

        return hash.ToHashCode();
    }
}
