using Tool.Collections;

namespace Tool.Graphics;

public readonly record struct BindingLayoutVector(
    BindingLayoutHandle Layout0 = default,
    BindingLayoutHandle Layout1 = default,
    BindingLayoutHandle Layout2 = default,
    BindingLayoutHandle Layout3 = default)
    //BindingLayoutHandle Layout4 = default,
    //BindingLayoutHandle Layout5 = default,
    //BindingLayoutHandle Layout6 = default,
    //BindingLayoutHandle Layout7 = default)
{
    public BindingLayoutHandle this[int index] =>
        index switch
        {
            0 => Layout0,
            1 => Layout1,
            2 => Layout2,
            3 => Layout3,
            //4 => Layout4,
            //5 => Layout5,
            //6 => Layout6,
            //7 => Layout7,
            _ => throw new IndexOutOfRangeException()
        };
}

public readonly record struct GraphicsPipelineDescription(
    ShaderHandle VertexShader = default,
    ShaderHandle PixelShader = default,
    InputLayoutHandle InputLayout = default,
    BindingLayoutVector BindingLayouts = default,
    RasterStateHandle RasterState = default,
    PrimitiveTopology Topology = PrimitiveTopology.TriangleList);

public readonly record struct GraphicsPipelineHandle(SlotHandle Raw)
{
    public static GraphicsPipelineHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
