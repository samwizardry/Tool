using Tool.Collections;

using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace Tool.Direct3D11;

internal readonly struct D3D11GraphicsPipeline
{
    public readonly PrimitiveTopology Topology;
    public readonly ID3D11InputLayout? InputLayout;
    public readonly ID3D11VertexShader? VertexShader;
    public readonly ID3D11PixelShader? PixelShader;
    public readonly ID3D11RasterizerState? RasterState;

    private D3D11GraphicsPipeline(
        PrimitiveTopology topology,
        ID3D11InputLayout? il,
        ID3D11VertexShader? vs,
        ID3D11PixelShader? ps,
        ID3D11RasterizerState? rs)
    {
        Topology = topology;
        InputLayout = il;
        VertexShader = vs;
        PixelShader = ps;
        RasterState = rs;
    }

    public void Destroy()
    {
    }

    public static D3D11GraphicsPipeline Create(in Graphics.GraphicsPipelineDescription desc,
        SlotMap<D3D11InputLayout> inputLayouts,
        SlotMap<D3D11Shader> shaders,
        SlotMap<D3D11RasterState> rasterStates)
    {
        var il = desc.InputLayout.IsValid
            ? inputLayouts.Get(desc.InputLayout.Raw).Native
            : null;

        var vs = desc.VertexShader.IsValid
            ? (ID3D11VertexShader)shaders.Get(desc.VertexShader.Raw).Native
            : null;

        var ps = desc.PixelShader.IsValid
            ? (ID3D11PixelShader)shaders.Get(desc.PixelShader.Raw).Native
            : null;

        var rs = desc.RasterState.IsValid
            ? rasterStates.Get(desc.RasterState.Raw).Native
            : null;

        return new D3D11GraphicsPipeline(desc.Topology.ToD3D11PrimitiveTopology(), il, vs, ps, rs);
    }

    public void Apply(ID3D11DeviceContext4 imContext)
    {
        imContext.IASetPrimitiveTopology(Topology);
        imContext.IASetInputLayout(InputLayout);
        imContext.VSSetShader(VertexShader);
        imContext.PSSetShader(PixelShader);
        imContext.RSSetState(RasterState);
    }
}
