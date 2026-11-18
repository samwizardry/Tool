namespace Torture.Graphics;

/// <summary>
/// Combines a <see cref="Graphics.GraphicsPipeline"/> with a concrete <see cref="Graphics.BindingSet"/>.
/// </summary>
public sealed class Material : IDisposable
{
    private bool _disposed;

    public Material(GraphicsPipeline graphicsPipeline, BindingSet bindingSet, Buffer? constantBuffer)
    {
        GraphicsPipeline = graphicsPipeline;
        BindingSet = bindingSet;
        ConstantBuffer = constantBuffer;
    }

    public GraphicsPipeline GraphicsPipeline { get; }
    public BindingSet BindingSet { get; private set; }
    public Buffer? ConstantBuffer { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        GraphicsPipeline.Dispose();
        BindingSet.Dispose();
        ConstantBuffer?.Dispose();

        _disposed = true;
    }
}
