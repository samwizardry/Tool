namespace Torture.Graphics.Direct3D11;

internal sealed class D3D11Texture : Core.Graphics.Texture
{
    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}
