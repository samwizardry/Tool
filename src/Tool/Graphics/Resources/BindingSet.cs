using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Tool.Collections;

namespace Tool.Graphics;

public readonly record struct BindingSetItem(
    uint Slot,
    BindingResourceType Type,
    SlotHandle Resource)
{
    public static BindingSetItem ConstantBuffer(uint slot, BufferHandle buffer) => new(slot, BindingResourceType.ConstantBuffer, buffer.Raw);
    public static BindingSetItem TextureSrv(uint slot, TextureHandle texture) => new(slot, BindingResourceType.TextureSrv, texture.Raw);
    public static BindingSetItem TextureUav(uint slot, TextureHandle texture) => new(slot, BindingResourceType.TextureUav, texture.Raw);
    public static BindingSetItem BufferSrv(uint slot, BufferHandle buffer) => new(slot, BindingResourceType.BufferSrv, buffer.Raw);
    public static BindingSetItem BufferUav(uint slot, BufferHandle buffer) => new(slot, BindingResourceType.BufferUav, buffer.Raw);
    public static BindingSetItem Sampler(uint slot, SamplerHandle sampler) => new(slot, BindingResourceType.Sampler, sampler.Raw);
}

public readonly record struct BindingSetDescription(BindingSetItem[] Bindings) : IEquatable<BindingSetDescription>
{
    public bool Equals(BindingSetDescription other) =>
        Bindings.AsSpan().SequenceEqual(other.Bindings.AsSpan());

    public override int GetHashCode()
    {
        HashCode hash = new();

        foreach (ref readonly var item in Bindings.AsSpan())
            hash.Add(item);

        return hash.ToHashCode();
    }
}

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

[InlineArray(Constants.MaxBindingLayouts)]
public struct BindingSetArray : IEquatable<BindingSetArray>
{
    private BindingSetHandle _binding0;

    public BindingSetArray(params ReadOnlySpan<BindingSetHandle> bindings)
    {
        if (bindings.Length > Constants.MaxBindingLayouts)
        {
            throw new ArgumentOutOfRangeException(nameof(bindings));
        }

        for (int i = 0; i < bindings.Length; i++)
        {
            this[i] = bindings[i];
        }
    }

    public readonly bool Equals(BindingSetArray other)
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
        obj is BindingSetArray other && Equals(other);

    public override readonly int GetHashCode()
    {
        HashCode hash = default;

        for (int i = 0; i < Constants.MaxBindingLayouts; i++)
        {
            hash.Add(this[i]);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(BindingSetArray left, BindingSetArray right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BindingSetArray left, BindingSetArray right)
    {
        return !(left == right);
    }
}
