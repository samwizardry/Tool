namespace Torture.Graphics;

//public struct GraphicsResourceHandle<T>
//    where T : GraphicsResource
//{
//    public T Handle;
//}

public sealed class GraphicsResourceFactory : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<int, RasterState> _rasterStates = [];
    private readonly Dictionary<int, InputLayout> _inputLayouts = [];
    private readonly Dictionary<int, Sampler> _samplers = [];

    private bool _disposed;

    public GraphicsResourceFactory(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public RasterState GetOrCreateRasterState(in RasterStateDescription desc)
    {
        int key = desc.GetHashCode();

        if (!_rasterStates.TryGetValue(key, out var rasterState))
        {
            rasterState = _graphicsDevice.CreateRasterState(in desc);
            _rasterStates[key] = rasterState;
        }

        return rasterState;
    }

    public InputLayout GetOrCreateInputLayout(in VertexLayout layout, Shader shader)
    {
        int key = layout.GetHashCode();

        if (!_inputLayouts.TryGetValue(key, out var inputLayout))
        {
            inputLayout = _graphicsDevice.CreateInputLayout(in layout, shader);
            _inputLayouts[key] = inputLayout;
        }

        return inputLayout;
    }

    public Sampler GetOrCreateSampler(in SamplerDescription desc)
    {
        int key = desc.GetHashCode();

        if (!_samplers.TryGetValue(key, out var sampler))
        {
            sampler = _graphicsDevice.CreateSampler(in desc);
            _samplers[key] = sampler;
        }

        return sampler;
    }

    // pipeline

    // bind layout

    // bind set

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var rasterState in _rasterStates.Values)
        {
            rasterState.Dispose();
        }

        foreach (var inputLayout in _inputLayouts.Values)
        {
            inputLayout.Dispose();
        }

        foreach (var sampler in _samplers.Values)
        {
            sampler.Dispose();
        }

        _disposed = true;
    }
}
