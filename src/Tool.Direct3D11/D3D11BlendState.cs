using Vortice.Direct3D11;

namespace Tool.Direct3D11;

internal readonly struct D3D11BlendState
{
    public readonly ID3D11BlendState1 Native;
    public readonly bool UseBlendConstants;

    private D3D11BlendState(ID3D11BlendState1 blendState, bool useBlendConstants)
    {
        Native = blendState;
        UseBlendConstants = useBlendConstants;
    }

    public void Destroy()
    {
        Native?.Dispose();
    }

    public static D3D11BlendState Create(ID3D11Device5 device, in Graphics.BlendStateDescription desc)
    {
        RenderTargetBlendDescription1[] renderTargets = new RenderTargetBlendDescription1[Graphics.Constants.MaxRenderTargets];
        bool independentBlendEnable = false;
        bool useBlendConstants = false;

        ref readonly var rt0 = ref desc.RenderTargets[0];

        for (int i = 0; i < Graphics.Constants.MaxRenderTargets; i++)
        {
            ref readonly var rt = ref desc.RenderTargets[i];

            if (rt.SrcBlend is Graphics.BlendFactor.ConstantColor or Graphics.BlendFactor.InvConstantColor ||
                rt.SrcBlendAlpha is Graphics.BlendFactor.ConstantColor or Graphics.BlendFactor.InvConstantColor ||
                rt.DestBlend is Graphics.BlendFactor.ConstantColor or Graphics.BlendFactor.InvConstantColor ||
                rt.DestBlendAlpha is Graphics.BlendFactor.ConstantColor or Graphics.BlendFactor.InvConstantColor)
            {
                useBlendConstants = true;
            }

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

        return new D3D11BlendState(device.CreateBlendState1(nativeDesc), useBlendConstants);
    }
}
