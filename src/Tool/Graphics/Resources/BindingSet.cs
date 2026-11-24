using Tool.Collections;

namespace Tool.Graphics;

public readonly record struct BindingSetItem(
    uint Slot,
    BindingResourceType Type,
    SlotHandle Resource)
{
    public static BindingSetItem ConstantBuffer(uint slot, BufferHandle buffer) => new(slot, BindingResourceType.ConstantBuffer, buffer.Raw);
    public static BindingSetItem TextureSrv(uint slot, TextureHandle texture) => new(slot, BindingResourceType.TextureSrv, texture.Raw);
    public static BindingSetItem TextureUav(uint slot, TextureHandle texture) => new(slot, BindingResourceType.TextureUav, texture.Raw);
    public static BindingSetItem BufferSrv(uint slot, BufferHandle buffer) => new(slot, BindingResourceType.BufferSrv, buffer.Raw);
    public static BindingSetItem BufferUav(uint slot, BufferHandle buffer) => new(slot, BindingResourceType.BufferUav, buffer.Raw);
    public static BindingSetItem Sampler(uint slot, SamplerHandle sampler) => new(slot, BindingResourceType.Sampler, sampler.Raw);
}

public readonly record struct BindingSetDescription(BindingSetItem[] Bindings) : IEquatable<BindingSetDescription>
{
    public bool Equals(BindingSetDescription other) =>
        Bindings.AsSpan().SequenceEqual(other.Bindings.AsSpan());

    public override int GetHashCode()
    {
        HashCode hash = new();

        foreach (ref readonly var item in Bindings.AsSpan())
            hash.Add(item);

        return hash.ToHashCode();
    }
}

public readonly record struct BindingSetHandle(SlotHandle Raw)
{
    public static BindingSetHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
