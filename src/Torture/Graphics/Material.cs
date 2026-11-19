namespace Torture.Graphics;

/// <summary>
/// Combines a <see cref="GraphicsPipeline"/> with a concrete <see cref="Graphics.BindingSet"/>.
/// </summary>
public sealed class Material : IDisposable
{
    private bool _disposed;

    public Material(GraphicsPipeline pipeline, BindingSet bindingSet, Buffer? constantBuffer)
    {
        Pipeline = pipeline;
        BindingSet = bindingSet;
        ConstantBuffer = constantBuffer;
    }

    public GraphicsPipeline Pipeline { get; }

    public BindingSet BindingSet { get; private set; }

    public Buffer? ConstantBuffer { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        BindingSet?.Dispose();
        ConstantBuffer?.Dispose();

        _disposed = true;
    }
}
