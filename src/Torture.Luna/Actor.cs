using Torture.Graphics;

namespace Torture.Luna;

public abstract class Actor : IDisposable
{
    protected readonly GraphicsDevice _graphicsDevice;

    protected bool _disposed;
    protected PerObjectData _perObjectData;

    private readonly Graphics.Buffer _perObject = default!;

    protected Actor(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _perObject = graphicsDevice.CreateBuffer(BufferDescription.Constant<PerObjectData>());
    }

    public virtual void Update(float dt)
    {
    }

    public virtual void Draw()
    {
        _graphicsDevice.UpdateBuffer(_perObject, ref _perObjectData);
        _graphicsDevice.SetConstantBuffer(_perObject, ShaderType.Vertex | ShaderType.Pixel, 1u);
    }

    public virtual void Dispose()
    {
        _perObject?.Dispose();
    }
}
