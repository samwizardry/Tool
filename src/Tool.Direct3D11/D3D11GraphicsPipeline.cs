using Tool.Collections;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace Tool.Direct3D11;

internal readonly struct D3D11GraphicsPipeline
{
    public readonly ID3D11VertexShader? VertexShader;
    public readonly ID3D11PixelShader? PixelShader;
    public readonly ID3D11InputLayout? InputLayout;
#if GRAPHICS_DEBUG
    public readonly Graphics.BindingLayoutArray BindingLayouts;
#endif
    public readonly ID3D11RasterizerState? RasterState;
    public readonly ID3D11BlendState1? BlendState;
    public readonly PrimitiveTopology Topology;

    public readonly bool UseBlendConstants;

    private D3D11GraphicsPipeline(
        ID3D11VertexShader? vs,
        ID3D11PixelShader? ps,
        ID3D11InputLayout? il,
#if GRAPHICS_DEBUG
        in Graphics.BindingLayoutArray bls,
#endif
        ID3D11RasterizerState? rs,
        ID3D11BlendState1? bs,
        PrimitiveTopology topology,
        bool useBlendConstants)
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
        UseBlendConstants = useBlendConstants;
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

        ID3D11BlendState1? bs = null;
        bool useBlendConstants = false;
        if (desc.BlendState.IsValid)
        {
            ref readonly var bsData = ref blendStates.Get(desc.BlendState.Raw);
            bs = bsData.Native;
            useBlendConstants = bsData.UseBlendConstants;
        }

        return new D3D11GraphicsPipeline(
            vs,
            ps,
            il,
#if GRAPHICS_DEBUG
            desc.BindingLayouts,
#endif
            rs,
            bs,
            desc.Topology.ToD3D11PrimitiveTopology(),
            useBlendConstants);
    }

    public void Apply(ID3D11DeviceContext4 imContext, Color4 blendConstantColor)
    {
        imContext.IASetPrimitiveTopology(Topology);
        imContext.IASetInputLayout(InputLayout);
        imContext.VSSetShader(VertexShader);
        imContext.PSSetShader(PixelShader);
        imContext.RSSetState(RasterState);
        imContext.OMSetBlendState(BlendState, blendConstantColor);
    }
}
