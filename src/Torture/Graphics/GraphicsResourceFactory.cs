namespace Torture.Graphics;

//public struct GraphicsResourceHandle<T>
//    where T : GraphicsResource
//{
//    public T Handle;
//}

public sealed class GraphicsResourceFactory
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<int, Sampler> _samplers = [];

    public GraphicsResourceFactory(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public Sampler GetSampler(in SamplerDescription desc)
    {
        int key = desc.GetHashCode();

        if (!_samplers.TryGetValue(key, out var sampler))
        {
            sampler = _graphicsDevice.CreateSampler(in desc);
            _samplers[key] = sampler;
        }

        return sampler;
    }
}
