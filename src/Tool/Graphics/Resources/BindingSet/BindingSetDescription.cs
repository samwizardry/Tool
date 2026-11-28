namespace Tool.Graphics;

/// <summary>
/// Describes a set of bindings corresponding to one binding layout.
/// </summary>
public readonly struct BindingSetDescription
{
    public readonly BindingSetItem[] Bindings;

    public BindingSetDescription(BindingSetItem[] bindings)
    {
        Bindings = bindings;
    }
}
