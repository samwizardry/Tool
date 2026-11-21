namespace Torture.Graphics;

public abstract class GraphicsResource : IDisposable
{
    protected bool _disposed;

    ~GraphicsResource()
    {
        Dispose(false);
    }

    public bool IsDispoed => _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            FreeManagedResources();
        }

        FreeUnmanagedResources();

        _disposed = true;
    }

    protected virtual void FreeManagedResources() { }
    protected virtual void FreeUnmanagedResources() { }
}
