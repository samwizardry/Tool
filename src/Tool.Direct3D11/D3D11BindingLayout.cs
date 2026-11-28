using Tool.Graphics;

namespace Tool.Direct3D11;

internal readonly struct D3D11BindingLayout
{
    public readonly ShaderType Visibility;

    public D3D11BindingLayout(in BindingLayoutDescription desc)
    {
        Visibility = desc.Visibility;
    }

#pragma warning disable CA1822 // Пометьте члены как статические
    public void Destroy()
#pragma warning restore CA1822 // Пометьте члены как статические
    {
    }
}
