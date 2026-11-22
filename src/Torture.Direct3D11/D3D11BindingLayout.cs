using Torture.Graphics;

namespace Torture.Direct3D11;

internal readonly struct D3D11BindingLayout
{
    public readonly BindingLayoutDescription Desc;

    public D3D11BindingLayout(in BindingLayoutDescription desc)
    {
        Desc = desc;
    }

    public ShaderType Visibility => Desc.Visibility;

    public void Destroy()
    {
    }
}
