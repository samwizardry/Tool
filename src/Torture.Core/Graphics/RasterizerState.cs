namespace Torture.Core.Graphics;

public abstract class RasterizerState : IDisposable
{
    protected readonly RasterizerStateDescription _desc;
    protected bool _disposed;

    protected RasterizerState(in RasterizerStateDescription desc)
    {
        _desc = desc;
    }

    public ref readonly RasterizerStateDescription Desc => ref _desc;

    public abstract void Dispose();
}
