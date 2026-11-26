using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Tool.Collections;

namespace Tool.Graphics;

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

[InlineArray(Constants.MaxBindingLayouts)]
public struct BindingLayoutArray : IEquatable<BindingLayoutArray>
{
    private BindingLayoutHandle _layout0;

    public BindingLayoutArray(params ReadOnlySpan<BindingLayoutHandle> layouts)
    {
        if (layouts.Length > Constants.MaxBindingLayouts)
        {
            throw new ArgumentOutOfRangeException(nameof(layouts));
        }

        for (int i = 0; i < layouts.Length; i++)
        {
            this[i] = layouts[i];
        }
    }

    public readonly bool Equals(BindingLayoutArray other)
    {
        for (int i = 0; i < Constants.MaxBindingLayouts; i++)
        {
            if (this[i] != other[i])
            {
                return false;
            }
        }

        return true;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj) =>
        obj is BindingLayoutArray other && Equals(other);

    public override readonly int GetHashCode()
    {
        HashCode hash = default;

        for (int i = 0; i < Constants.MaxBindingLayouts; i++)
        {
            hash.Add(this[i]);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(BindingLayoutArray left, BindingLayoutArray right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BindingLayoutArray left, BindingLayoutArray right)
    {
        return !(left == right);
    }
}
