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
    PrimitiveTopology Topology = PrimitiveTopology.TriangleList) : IEquatable<GraphicsPipelineDescription>
{
    public bool Equals(GraphicsPipelineDescription other)
    {
        for (int i = 0; i < Constants.MaxBindingLayouts; i++)
        {
            if (BindingLayouts[i] != other.BindingLayouts[i])
            {
                return false;
            }
        }

        return
            VertexShader == other.VertexShader &&
            PixelShader == other.PixelShader &&
            InputLayout == other.InputLayout &&
            RasterState == other.RasterState &&
            Topology == other.Topology;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(VertexShader);
        hash.Add(PixelShader);
        hash.Add(InputLayout);
        hash.Add(RasterState);
        hash.Add(Topology);

        for (int i = 0; i < Constants.MaxBindingLayouts; i++)
        {
            hash.Add(BindingLayouts[i]);
        }

        return hash.ToHashCode();
    }
}

public readonly record struct GraphicsPipelineHandle(SlotHandle Raw)
{
    public static GraphicsPipelineHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
