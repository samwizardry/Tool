using Torture.Collections;

namespace Torture.Graphics;

public readonly record struct BindingLayoutItem(
    uint Slot,
    BindingResourceType Type) : IEquatable<BindingLayoutItem>
{
    public static BindingLayoutItem ConstantBuffer(uint slot) => new(slot, BindingResourceType.ConstantBuffer);
    public static BindingLayoutItem TextureSrv(uint slot) => new(slot, BindingResourceType.TextureSrv);
    public static BindingLayoutItem TextureUav(uint slot) => new(slot, BindingResourceType.TextureUav);
    public static BindingLayoutItem BufferSrv(uint slot) => new(slot, BindingResourceType.BufferSrv);
    public static BindingLayoutItem BufferUav(uint slot) => new(slot, BindingResourceType.BufferUav);
    public static BindingLayoutItem Sampler(uint slot) => new(slot, BindingResourceType.Sampler);
}

public readonly record struct BindingLayoutDescription(
    ShaderType Visibility,
    BindingLayoutItem[] Bindings,
    uint RegisterSpace = 0) : IEquatable<BindingLayoutDescription>
{
    public bool Equals(BindingLayoutDescription other) =>
        Visibility == other.Visibility &&
        RegisterSpace == other.RegisterSpace &&
        Bindings.AsSpan().SequenceEqual(other.Bindings.AsSpan());

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Visibility);
        hash.Add(RegisterSpace);

        foreach (ref readonly var item in Bindings.AsSpan())
            hash.Add(item);

        return hash.ToHashCode();
    }
}

public readonly record struct BindingLayoutHandle(SlotHandle Raw)
{
    public static BindingLayoutHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}