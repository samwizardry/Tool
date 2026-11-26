using Vortice.Direct3D11;

namespace Tool.Direct3D11;

internal readonly struct D3D11BlendState
{
    public readonly ID3D11BlendState1 Native;

    private D3D11BlendState(ID3D11BlendState1 blendState)
    {
        Native = blendState;
    }

    public void Destroy()
    {
        Native?.Dispose();
    }

    public static D3D11BlendState Create(ID3D11Device5 device, in Graphics.BlendStateDescription desc)
    {
        RenderTargetBlendDescription1[] renderTargets = new RenderTargetBlendDescription1[Graphics.Constants.MaxRenderTargets];
        bool independentBlendEnable = false;

        var rt0 = desc[0];

        for (int i = 0; i < Graphics.Constants.MaxRenderTargets; i++)
        {
            Graphics.BlendStateDescription.RenderTarget rt = desc[i];

            if (i > 0 && rt != rt0)
            {
                independentBlendEnable = true;
            }

            renderTargets[i] = new RenderTargetBlendDescription1
            {
                BlendEnable = rt.BlendEnable,
                LogicOpEnable = false,
                SourceBlend = rt.SrcBlend.ToD3D11Blend(),
                DestinationBlend = rt.DestBlend.ToD3D11Blend(),
                BlendOperation = rt.BlendOp.ToD3D11BlendOp(),
                SourceBlendAlpha = rt.SrcBlendAlpha.ToD3D11Blend(),
                DestinationBlendAlpha = rt.DestBlendAlpha.ToD3D11Blend(),
                BlendOperationAlpha = rt.BlendOpAlpha.ToD3D11BlendOp(),
                LogicOp = LogicOp.Noop,
                RenderTargetWriteMask = (ColorWriteEnable)rt.ColorWriteMask
            };
        }

        BlendDescription1 nativeDesc = new()
        {
            RenderTarget = renderTargets,
            AlphaToCoverageEnable = desc.AlphaToCoverageEnable,
            IndependentBlendEnable = independentBlendEnable
        };

        return new D3D11BlendState(device.CreateBlendState1(nativeDesc));
    }
}
