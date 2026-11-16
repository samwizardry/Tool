namespace Torture.Core.Graphics;

public abstract class RasterState : IDisposable
{
    protected readonly RasterStateDescription _desc;
    protected bool _disposed;

    protected RasterState(in RasterStateDescription desc)
    {
        _desc = desc;
    }

    public ref readonly RasterStateDescription Desc => ref _desc;

    public abstract void Dispose();
}
