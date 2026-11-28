using Tool.Collections;

namespace Tool.Graphics;

public readonly record struct BindingSetItem
{
    public readonly uint Slot;
    public readonly BindingResourceType Type;
    public readonly SlotHandle Resource;

    public BindingSetItem(
        uint slot,
        BindingResourceType type,
        SlotHandle resource)
    {
        Slot = slot;
        Type = type;
        Resource = resource;
    }

    public static BindingSetItem ConstantBuffer(uint slot, BufferHandle buffer) => new BindingSetItem(slot, BindingResourceType.ConstantBuffer, buffer.Raw);
    public static BindingSetItem TextureSrv(uint slot, TextureHandle texture) => new BindingSetItem(slot, BindingResourceType.TextureSrv, texture.Raw);
    public static BindingSetItem TextureUav(uint slot, TextureHandle texture) => new BindingSetItem(slot, BindingResourceType.TextureUav, texture.Raw);
    public static BindingSetItem BufferSrv(uint slot, BufferHandle buffer) => new BindingSetItem(slot, BindingResourceType.BufferSrv, buffer.Raw);
    public static BindingSetItem BufferUav(uint slot, BufferHandle buffer) => new BindingSetItem(slot, BindingResourceType.BufferUav, buffer.Raw);
    public static BindingSetItem Sampler(uint slot, SamplerHandle sampler) => new BindingSetItem(slot, BindingResourceType.Sampler, sampler.Raw);
}
