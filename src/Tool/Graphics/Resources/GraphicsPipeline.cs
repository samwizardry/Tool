using Tool.Collections;

namespace Tool.Graphics;

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
        in BindingLayoutArray bindingLayouts = default,
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
