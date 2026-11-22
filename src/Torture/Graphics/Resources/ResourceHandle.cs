namespace Torture.Graphics;

public readonly record struct ResourceHandle(uint Index, uint Generation)
{
    public static ResourceHandle Invalid => default;
    public bool IsValid => Generation != 0;
}
