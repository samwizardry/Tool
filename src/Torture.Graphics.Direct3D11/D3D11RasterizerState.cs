namespace Torture.Graphics.Direct3D11;

using Vortice.Direct3D11;

using CoreGfx = Core.Graphics;

public class D3D11RasterizerState : CoreGfx.RasterizerState
{
    public D3D11RasterizerState(ID3D11Device5 device, in CoreGfx.RasterizerStateDescription desc)
        : base(in desc)
    {
        RasterizerDescription d3dDesc = new()
        {
            FillMode = desc.FillMode switch
            {
                CoreGfx.RasterFillMode.Solid => FillMode.Solid,
                CoreGfx.RasterFillMode.Wireframe => FillMode.Wireframe,
                _ => throw new NotSupportedException()
            },
            CullMode = desc.CullMode switch
            {
                CoreGfx.RasterCullMode.Back => CullMode.Back,
                CoreGfx.RasterCullMode.Front => CullMode.Front,
                CoreGfx.RasterCullMode.None => CullMode.None,
                _ => throw new NotSupportedException()
            },
            FrontCounterClockwise = desc.FrontCounterClockwise,
            DepthBias = desc.DepthBias,
            DepthBiasClamp = desc.DepthBiasClamp,
            SlopeScaledDepthBias = desc.SlopeScaledDepthBias,
            DepthClipEnable = desc.DepthClipEnable,
            ScissorEnable = desc.ScissorEnable,
            MultisampleEnable = desc.MultisampleEnable,
            AntialiasedLineEnable = desc.AntialiasedLineEnable
        };

        bool extendedState = desc.ConservativeRasterEnable
                          || desc.ForcedSampleCount > 0
                          || desc.ProgrammableSamplePositionsEnable
                          || desc.QuadFillEnable;

        if (extendedState)
        {
            RasterizerDescription2 d3dExtendedDesc = new()
            {
                FillMode = d3dDesc.FillMode,
                CullMode = d3dDesc.CullMode,
                FrontCounterClockwise = d3dDesc.FrontCounterClockwise,
                DepthBias = d3dDesc.DepthBias,
                DepthBiasClamp = d3dDesc.DepthBiasClamp,
                SlopeScaledDepthBias = d3dDesc.SlopeScaledDepthBias,
                DepthClipEnable = d3dDesc.DepthClipEnable,
                ScissorEnable = d3dDesc.ScissorEnable,
                MultisampleEnable = d3dDesc.MultisampleEnable,
                AntialiasedLineEnable = d3dDesc.AntialiasedLineEnable,
                ForcedSampleCount = desc.ForcedSampleCount,
                ConservativeRaster = desc.ConservativeRasterEnable ? ConservativeRasterizationMode.On : ConservativeRasterizationMode.Off
            };

            Native = device.CreateRasterizerState2(d3dExtendedDesc);
        }
        else
        {
            Native = device.CreateRasterizerState(d3dDesc);
        }
    }

    public ID3D11RasterizerState Native { get; }

    public override void Dispose()
    {
        if (_disposed)
            return;

        Native?.Dispose();

        _disposed = true;
    }
}
