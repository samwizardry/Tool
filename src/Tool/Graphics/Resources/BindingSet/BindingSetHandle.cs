using Tool.Collections;

namespace Tool.Graphics;

public readonly record struct BindingSetHandle
{
    public readonly SlotHandle Raw;

    public BindingSetHandle(SlotHandle raw)
    {
        Raw = raw;
    }

    public static BindingSetHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
