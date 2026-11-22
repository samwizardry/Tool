namespace Torture.Graphics;

public readonly record struct InputLayoutHandle(ResourceHandle Raw)
{
    public static InputLayoutHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
