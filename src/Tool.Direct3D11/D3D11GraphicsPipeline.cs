using Tool.Collections;

using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace Tool.Direct3D11;

internal readonly struct D3D11GraphicsPipeline
{
    public readonly ID3D11VertexShader? VertexShader;
    public readonly ID3D11PixelShader? PixelShader;
    public readonly ID3D11InputLayout? InputLayout;
#if GRAPHICS_DEBUG
    public readonly Graphics.BindingLayoutVector BindingLayouts;
#endif
    public readonly ID3D11RasterizerState? RasterState;
    public readonly ID3D11BlendState1? BlendState;
    public readonly PrimitiveTopology Topology;

    private D3D11GraphicsPipeline(
        ID3D11VertexShader? vs,
        ID3D11PixelShader? ps,
        ID3D11InputLayout? il,
#if GRAPHICS_DEBUG
        in Graphics.BindingLayoutVector bls,
#endif
        ID3D11RasterizerState? rs,
        ID3D11BlendState1? bs,
        PrimitiveTopology topology)
    {
        VertexShader = vs;
        PixelShader = ps;
        InputLayout = il;
#if GRAPHICS_DEBUG
        BindingLayouts = bls;
#endif
        RasterState = rs;
        BlendState = bs;
        Topology = topology;
    }

    public void Destroy()
    {
    }

    public static D3D11GraphicsPipeline Create(in Graphics.GraphicsPipelineDescription desc,
        SlotMap<D3D11Shader> shaders,
        SlotMap<D3D11InputLayout> inputLayouts,
        SlotMap<D3D11RasterState> rasterStates,
        SlotMap<D3D11BlendState> blendStates)
    {
        var vs = desc.VertexShader.IsValid
            ? (ID3D11VertexShader)shaders.Get(desc.VertexShader.Raw).Native
            : null;

        var ps = desc.PixelShader.IsValid
            ? (ID3D11PixelShader)shaders.Get(desc.PixelShader.Raw).Native
            : null;

        var il = desc.InputLayout.IsValid
            ? inputLayouts.Get(desc.InputLayout.Raw).Native
            : null;

        var rs = desc.RasterState.IsValid
            ? rasterStates.Get(desc.RasterState.Raw).Native
            : null;

        var bs = desc.BlendState.IsValid
            ? blendStates.Get(desc.BlendState.Raw).Native
            : null;

        return new D3D11GraphicsPipeline(
            vs,
            ps,
            il,
#if GRAPHICS_DEBUG
            desc.BindingLayouts,
#endif
            rs,
            bs,
            desc.Topology.ToD3D11PrimitiveTopology());
    }

    public void Apply(ID3D11DeviceContext4 imContext)
    {
        imContext.IASetPrimitiveTopology(Topology);
        imContext.IASetInputLayout(InputLayout);
        imContext.VSSetShader(VertexShader);
        imContext.PSSetShader(PixelShader);
        imContext.RSSetState(RasterState);
        imContext.OMSetBlendState(BlendState);
    }
}
