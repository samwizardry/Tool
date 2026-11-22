using Torture.Graphics;

using Vortice.Direct3D11;

namespace Torture.Direct3D11;

internal readonly struct D3D11InputLayout
{
    public readonly ID3D11InputLayout Native;

    private D3D11InputLayout(ID3D11InputLayout inputLayout)
    {
        Native = inputLayout;
    }

    public void Destroy()
    {
        Native?.Dispose();
    }

    public static D3D11InputLayout Create(ID3D11Device5 device, in VertexLayout layout, in D3D11Shader shader)
    {
        if (shader.ShaderType != ShaderType.Vertex)
        {
            throw new InvalidOperationException();
        }

        InputElementDescription[] nativeDescription = new InputElementDescription[layout.Elements.Length];

        for (int i = 0; i < layout.Elements.Length; ++i)
        {
            VertexElement element = layout.Elements[i];

            nativeDescription[i].SemanticName = element.SemanticName.ToD3D11SemanticName();
            nativeDescription[i].SemanticIndex = element.SemanticIndex;
            nativeDescription[i].Format = element.Format.ToDXGIFormat();
            nativeDescription[i].Slot = 0;
            nativeDescription[i].AlignedByteOffset = element.ByteOffset;
            nativeDescription[i].Classification = InputClassification.PerVertexData;
            nativeDescription[i].InstanceDataStepRate = 0;
        }

        return new D3D11InputLayout(device.CreateInputLayout(nativeDescription, shader.Bytecode!));
    }
}
