namespace Torture.Graphics;

public readonly record struct GraphicsPipelineDescription(
    ShaderHandle VertexShader = default,
    ShaderHandle PixelShader = default,
    InputLayoutHandle InputLayout = default,
    RasterStateHandle RasterState = default,
    PrimitiveTopology Topology = PrimitiveTopology.TriangleList,
    BindingLayoutHandle[]? BindingLayouts = null) : IEquatable<GraphicsPipelineDescription>
{
    public bool Equals(GraphicsPipelineDescription other) =>
        VertexShader == other.VertexShader &&
        PixelShader == other.PixelShader &&
        InputLayout == other.InputLayout &&
        RasterState == other.RasterState &&
        Topology == other.Topology &&
        BindingLayouts.AsSpan().SequenceEqual(other.BindingLayouts.AsSpan());

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(VertexShader);
        hash.Add(PixelShader);
        hash.Add(InputLayout);
        hash.Add(RasterState);
        hash.Add(Topology);

        foreach (var layout in BindingLayouts.AsSpan())
            hash.Add(layout);

        return hash.ToHashCode();
    }
}

public readonly record struct GraphicsPipelineHandle(ResourceHandle Raw)
{
    public static GraphicsPipelineHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
