using Vortice.Direct3D11;

using TortGfx = Torture.Graphics;

namespace Torture.Direct3D11;

internal sealed class D3D11InputLayout : TortGfx.InputLayout
{
    public D3D11InputLayout(ID3D11Device5 device, in TortGfx.VertexLayout layout, D3D11Shader shader)
    {
        if (shader.ShaderType != TortGfx.ShaderType.Vertex)
        {
            throw new InvalidOperationException();
        }

        InputElementDescription[] nativeDescription = new InputElementDescription[layout.Elements.Length];

        for (int i = 0; i < layout.Elements.Length; ++i)
        {
            TortGfx.VertexElement element = layout.Elements[i];

            nativeDescription[i].SemanticName = element.SemanticName.ToD3D11SemanticName();
            nativeDescription[i].SemanticIndex = element.SemanticIndex;
            nativeDescription[i].Format = element.Format.ToDXGIFormat();
            nativeDescription[i].Slot = 0;
            nativeDescription[i].AlignedByteOffset = element.ByteOffset;
            nativeDescription[i].Classification = InputClassification.PerVertexData;
            nativeDescription[i].InstanceDataStepRate = 0;
        }

        Native = device.CreateInputLayout(nativeDescription, shader.Bytecode!);
    }

    public ID3D11InputLayout Native { get; private set; }

    public override void Dispose()
    {
        if (_disposed)
            return;

        Native?.Dispose();
        Native = null!;

        _disposed = true;
    }
}
