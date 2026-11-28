namespace Tool.Graphics;

public readonly record struct BindingLayoutDescription : IEquatable<BindingLayoutDescription>
{
    public readonly ShaderType Visibility;
    public readonly BindingLayoutItem[] Bindings;
    public readonly uint RegisterSpace;

    public BindingLayoutDescription(
        ShaderType visibility,
        BindingLayoutItem[] bindings,
        uint registerSpace = 0)
    {
        Visibility = visibility;
        Bindings = bindings;
        RegisterSpace = registerSpace;
    }

    public bool Equals(BindingLayoutDescription other) =>
        Visibility == other.Visibility &&
        RegisterSpace == other.RegisterSpace &&
        Bindings.AsSpan().SequenceEqual(other.Bindings.AsSpan());

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Visibility);
        hash.Add(RegisterSpace);

        for (int i = 0; i < Bindings.Length; i++)
        {
            hash.Add(Bindings[i]);
        }

        return hash.ToHashCode();
    }
}
