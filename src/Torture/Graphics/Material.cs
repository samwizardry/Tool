namespace Torture.Graphics;

/// <summary>
/// Combines a <see cref="GraphicsPipeline"/> with a concrete <see cref="Graphics.BindingSet"/>.
/// </summary>
public sealed class Material : IDisposable
{
    private bool _disposed;

    public Material(GraphicsPipeline pipeline, BindingSet? bindingSet = null, Buffer? constantBuffer = null)
    {
        Pipeline = pipeline;
        BindingSet = bindingSet;
        ConstantBuffer = constantBuffer;
    }

    public GraphicsPipeline Pipeline { get; }

    public BindingSet? BindingSet { get; }

    public Buffer? ConstantBuffer { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}
