using Torture.Graphics;

namespace Torture.Luna;

public abstract class Actor : IDisposable
{
    protected readonly GraphicsDevice _device;

    protected bool _disposed;
    protected PerObjectData _perObjectData;

    protected Actor(GraphicsDevice device)
    {
        _device = device;
    }

    public virtual void Update(float dt)
    {
    }

    public virtual void Draw()
    {
        _device.UpdateBuffer(PipelineStates.PerObjectCb, ref _perObjectData);
    }

    public virtual void Dispose()
    {
    }
}
