using TortGfx = Torture.Graphics;

namespace Torture.Direct3D11;

internal sealed class D3D11BindingLayout : TortGfx.BindingLayout
{
    public D3D11BindingLayout(in TortGfx.BindingLayoutDescription desc)
        : base(desc)
    {
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}
