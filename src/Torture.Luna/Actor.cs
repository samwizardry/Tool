using Torture.Graphics;

namespace Torture.Luna;

public abstract class Actor : IDisposable
{
    protected readonly GraphicsDevice _device;
    protected readonly PhongPerObjectData _actorData;

    protected bool _disposed;

    protected Actor(GraphicsDevice device, Graphics.Buffer buffer)
    {
        _device = device;
        _actorData = new PhongPerObjectData(buffer);
    }

    public virtual void Update(float dt)
    {
    }

    public virtual void Draw()
    {
    }

    public virtual void Dispose()
    {
    }
}
