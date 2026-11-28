namespace Tool.Graphics;

public readonly record struct BindingLayoutItem : IEquatable<BindingLayoutItem>
{
    public readonly uint Slot;
    public readonly BindingResourceType Type;

    public BindingLayoutItem(uint slot, BindingResourceType type)
    {
        Slot = slot;
        Type = type;
    }

    public static BindingLayoutItem ConstantBuffer(uint slot) => new BindingLayoutItem(slot, BindingResourceType.ConstantBuffer);
    public static BindingLayoutItem TextureSrv(uint slot) => new BindingLayoutItem(slot, BindingResourceType.TextureSrv);
    public static BindingLayoutItem TextureUav(uint slot) => new BindingLayoutItem(slot, BindingResourceType.TextureUav);
    public static BindingLayoutItem BufferSrv(uint slot) => new BindingLayoutItem(slot, BindingResourceType.BufferSrv);
    public static BindingLayoutItem BufferUav(uint slot) => new BindingLayoutItem(slot, BindingResourceType.BufferUav);
    public static BindingLayoutItem Sampler(uint slot) => new BindingLayoutItem(slot, BindingResourceType.Sampler);
}
