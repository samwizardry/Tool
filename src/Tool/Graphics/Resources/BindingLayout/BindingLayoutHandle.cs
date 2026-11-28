using Tool.Collections;

namespace Tool.Graphics;

public readonly record struct BindingLayoutHandle
{
    public readonly SlotHandle Raw;

    public BindingLayoutHandle(SlotHandle raw)
    {
        Raw = raw;
    }

    public static BindingLayoutHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
