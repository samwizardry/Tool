using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Tool.Collections;

namespace Tool.Graphics;

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
        HashCode hash = new HashCode();
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

public readonly record struct GraphicsPipelineDescription
{
    public readonly ShaderHandle VertexShader;
    public readonly ShaderHandle PixelShader;
    public readonly InputLayoutHandle InputLayout;
    public readonly BindingLayoutArray BindingLayouts;
    public readonly RasterStateHandle RasterState;
    public readonly BlendStateHandle BlendState;
    public readonly PrimitiveTopology Topology;

    public GraphicsPipelineDescription(
        ShaderHandle vertexShader = default,
        ShaderHandle pixelShader = default,
        InputLayoutHandle inputLayout = default,
        BindingLayoutArray bindingLayouts = default,
        RasterStateHandle rasterState = default,
        BlendStateHandle blendState = default,
        PrimitiveTopology topology = PrimitiveTopology.TriangleList)
    {
        VertexShader = vertexShader;
        PixelShader = pixelShader;
        InputLayout = inputLayout;
        BindingLayouts = bindingLayouts;
        RasterState = rasterState;
        BlendState = blendState;
        Topology = topology;
    }
}

public readonly record struct GraphicsPipelineHandle
{
    public readonly SlotHandle Raw;

    public GraphicsPipelineHandle(SlotHandle raw)
    {
        Raw = raw;
    }

    public static GraphicsPipelineHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
