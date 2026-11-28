using Tool.Collections;

namespace Tool.Graphics.BlendState;

public readonly record struct BlendStateHandle
{
    public readonly SlotHandle Raw;

    public BlendStateHandle(SlotHandle raw)
    {
        Raw = raw;
    }

    public static BlendStateHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
