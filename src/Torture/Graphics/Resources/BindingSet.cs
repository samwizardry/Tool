namespace Torture.Graphics;

/// <summary>
/// Binds a concrete GPU resource (<see cref="Buffer"/>, <see cref="Texture"/>, or <see cref="Sampler"/>) to a shader slot.
/// </summary>
public readonly record struct BindingSetItem(
    uint Slot,
    BindingResourceType Type,
    IBindableResource Resource)
{
    public static BindingSetItem ConstantBuffer(uint slot, Buffer buffer) => new(slot, BindingResourceType.ConstantBuffer, buffer);
    public static BindingSetItem TextureSrv(uint slot, Texture texture) => new(slot, BindingResourceType.TextureSrv, texture);
    public static BindingSetItem TextureUav(uint slot, Texture texture) => new(slot, BindingResourceType.TextureUav, texture);
    /// <summary>
    /// Binds a structured or raw buffer as a Shader Resource View (e.g. instance transforms for DrawInstanced/DrawIndirect).
    /// </summary>
    public static BindingSetItem BufferSrv(uint slot, Buffer buffer) => new(slot, BindingResourceType.BufferSrv, buffer);
    /// <summary>
    /// Binds a structured, raw, or indirect arguments buffer as an Unordered Access View (e.g. for compute culling).
    /// </summary>
    public static BindingSetItem BufferUav(uint slot, Buffer buffer) => new(slot, BindingResourceType.BufferUav, buffer);
    public static BindingSetItem Sampler(uint slot, Sampler sampler) => new(slot, BindingResourceType.Sampler, sampler);
}

/// <summary>
/// Describes a collection of concrete resources matching a <see cref="BindingLayout"/>.
/// </summary>
public readonly record struct BindingSetDescription(BindingSetItem[] Bindings);

public abstract class BindingSet : GraphicsResource
{
    private readonly BindingSetDescription _desc;

    protected BindingSet(in BindingSetDescription desc)
    {
        _desc = desc;
    }

    public ref readonly BindingSetDescription Desc => ref _desc;
}
