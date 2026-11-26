using Tool.Collections;

namespace Tool.Graphics;

public readonly record struct GraphicsPipelineDescription(
    ShaderHandle VertexShader = default,
    ShaderHandle PixelShader = default,
    InputLayoutHandle InputLayout = default,
    BindingLayoutArray BindingLayouts = default,
    RasterStateHandle RasterState = default,
    BlendStateHandle BlendState = default,
    PrimitiveTopology Topology = PrimitiveTopology.TriangleList);

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
