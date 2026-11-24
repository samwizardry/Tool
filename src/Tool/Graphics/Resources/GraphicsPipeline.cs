using Tool.Collections;

namespace Tool.Graphics;

public readonly record struct BindingLayoutVector
{
    public readonly BindingLayoutHandle Layout0;
    public readonly BindingLayoutHandle Layout1;
    public readonly BindingLayoutHandle Layout2;
    public readonly BindingLayoutHandle Layout3;
    //public readonly BindingLayoutHandle Layout4;
    //public readonly BindingLayoutHandle Layout5;
    //public readonly BindingLayoutHandle Layout6;
    //public readonly BindingLayoutHandle Layout7;

    public BindingLayoutVector(
        BindingLayoutHandle layout0 = default,
        BindingLayoutHandle layout1 = default,
        BindingLayoutHandle layout2 = default,
        BindingLayoutHandle layout3 = default)
        //BindingLayoutHandle layout4 = default,
        //BindingLayoutHandle layout5 = default,
        //BindingLayoutHandle layout6 = default,
        //BindingLayoutHandle layout7 = default)
    {
        Layout0 = layout0;
        Layout1 = layout1;
        Layout2 = layout2;
        Layout3 = layout3;
        //Layout4 = layout4;
        //Layout5 = layout5;
        //Layout6 = layout6;
        //Layout7 = layout7;
    }

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
