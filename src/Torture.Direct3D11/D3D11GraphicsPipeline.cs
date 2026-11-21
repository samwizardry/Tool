using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace Torture.Direct3D11;

internal sealed class D3D11GraphicsPipeline : CoreGraphicsPipeline
{
    private readonly ID3D11VertexShader? _vertexShader;
    private readonly ID3D11PixelShader? _pixelShader;
    private readonly ID3D11InputLayout? _inputLayout;
    private readonly ID3D11RasterizerState? _rasterState;
    private readonly PrimitiveTopology _topology;

    public D3D11GraphicsPipeline(in CoreGraphicsPipelineDesc desc)
        : base(desc)
    {
        if (desc.VertexShader is D3D11Shader vs)
        {
            _vertexShader = (ID3D11VertexShader)vs.Native;
        }
        if (desc.PixelShader is D3D11Shader ps)
        {
            _pixelShader = (ID3D11PixelShader)ps.Native;
        }
        if (desc.InputLayout is D3D11InputLayout inputLayout)
        {
            _inputLayout = inputLayout.Native;
        }
        if (desc.RasterState is D3D11RasterState rasterState)
        {
            _rasterState = rasterState.Native;
        }

        _topology = desc.Topology.ToD3D11PrimitiveTopology();
    }

    public void Apply(ID3D11DeviceContext4 imContext)
    {
        imContext.IASetPrimitiveTopology(_topology);
        imContext.IASetInputLayout(_inputLayout);
        imContext.VSSetShader(_vertexShader);
        imContext.PSSetShader(_pixelShader);
        imContext.RSSetState(_rasterState);
    }
}
