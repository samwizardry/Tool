namespace Torture.Graphics;

/// <summary>
/// Describes a single resource slot in a <see cref="BindingLayoutDescription"/>.
/// </summary>
public readonly record struct BindingLayoutItem(
    uint Slot,
    BindingResourceType Type)
{
    public static BindingLayoutItem ConstantBuffer(uint slot) => new(slot, BindingResourceType.ConstantBuffer);
    public static BindingLayoutItem TextureSrv(uint slot) => new(slot, BindingResourceType.TextureSrv);
    public static BindingLayoutItem TextureUav(uint slot) => new(slot, BindingResourceType.TextureUav);
    public static BindingLayoutItem BufferSrv(uint slot) => new(slot, BindingResourceType.BufferSrv);
    public static BindingLayoutItem BufferUav(uint slot) => new(slot, BindingResourceType.BufferUav);
    public static BindingLayoutItem Sampler(uint slot) => new(slot, BindingResourceType.Sampler);
}

/// <summary>
/// Describes the layout contract of shader resource bindings for a specific binding space/set.
/// </summary>
public readonly record struct BindingLayoutDescription(
    ShaderType Visibility,
    BindingLayoutItem[] Bindings,
    uint RegisterSpace = 0);

/// <summary>
/// Represents a compiled resource binding layout schema.
/// </summary>
public abstract class BindingLayout : IDisposable
{
    private readonly BindingLayoutDescription _desc;
    protected bool _disposed;

    protected BindingLayout(in BindingLayoutDescription desc)
    {
        _desc = desc;
    }

    public ref readonly BindingLayoutDescription Desc => ref _desc;

    public abstract void Dispose();
}
