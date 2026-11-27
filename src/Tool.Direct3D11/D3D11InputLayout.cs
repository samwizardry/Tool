using Tool.Graphics;

using Vortice.Direct3D11;

namespace Tool.Direct3D11;

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

        var nativeDescription = new InputElementDescription[layout.Count];

        for (int i = 0; i < layout.Count; ++i)
        {
            var element = layout[i];
            nativeDescription[i].SemanticName = element.SemanticName.ToD3D11SemanticName();
            nativeDescription[i].SemanticIndex = element.SemanticIndex;
            nativeDescription[i].Format = element.Format.ToDXGIFormat();
            nativeDescription[i].Slot = 0;
            nativeDescription[i].AlignedByteOffset = element.ByteOffset;
            nativeDescription[i].Classification = InputClassification.PerVertexData;
            nativeDescription[i].InstanceDataStepRate = 0;
        }

        var il = device.CreateInputLayout(nativeDescription, shader.Bytecode!);
        return new D3D11InputLayout(il);
    }
}
