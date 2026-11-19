namespace Torture.Graphics;

/// <summary>
/// Describes a graphics pipeline state configuration.
/// </summary>
public readonly record struct GraphicsPipelineDescription(
    Shader? VertexShader = null,
    Shader? PixelShader = null,
    InputLayout? InputLayout = null,
    RasterState? RasterState = null,
    PrimitiveTopology Topology = PrimitiveTopology.TriangleList,
    BindingLayout[]? BindingLayouts = null);

/// <summary>
/// Represents an immutable graphics pipeline state object (PSO).
/// </summary>
public abstract class GraphicsPipeline : IDisposable
{
    protected readonly GraphicsPipelineDescription _desc;
    protected bool _disposed;

    protected GraphicsPipeline(in GraphicsPipelineDescription desc)
    {
        _desc = desc;
    }

    public ref readonly GraphicsPipelineDescription Desc => ref _desc;

    public abstract void Dispose();
}
