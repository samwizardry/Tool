using Torture.Collections;

namespace Torture.Graphics;

public readonly record struct InputLayoutHandle(SlotHandle Raw)
{
    public static InputLayoutHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
