// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Vortice.Direct3D11;

namespace Torture.Direct3D11;

internal readonly struct D3D11RasterState
{
    public readonly ID3D11RasterizerState Native;

    private D3D11RasterState(ID3D11RasterizerState rasterState)
    {
        Native = rasterState;
    }

    public void Destroy()
    {
        Native?.Dispose();
    }

    public static D3D11RasterState Create(ID3D11Device5 device, in Graphics.RasterStateDescription desc)
    {
        var rasterState = device.CreateRasterizerState2(new()
        {
            FillMode = desc.FillMode switch
            {
                Graphics.RasterFillMode.Solid => FillMode.Solid,
                Graphics.RasterFillMode.Wireframe => FillMode.Wireframe,
                _ => throw new NotSupportedException()
            },
            CullMode = desc.CullMode switch
            {
                Graphics.RasterCullMode.Back => CullMode.Back,
                Graphics.RasterCullMode.Front => CullMode.Front,
                Graphics.RasterCullMode.None => CullMode.None,
                _ => throw new NotSupportedException()
            },
            FrontCounterClockwise = desc.FrontCounterClockwise,
            DepthBias = desc.DepthBias,
            DepthBiasClamp = desc.DepthBiasClamp,
            SlopeScaledDepthBias = desc.SlopeScaledDepthBias,
            DepthClipEnable = desc.DepthClipEnable,
            ScissorEnable = desc.ScissorEnable,
            MultisampleEnable = desc.MultisampleEnable,
            AntialiasedLineEnable = desc.AntialiasedLineEnable,
            ForcedSampleCount = desc.ForcedSampleCount,
            ConservativeRaster = desc.ConservativeRasterEnable ? ConservativeRasterizationMode.On : ConservativeRasterizationMode.Off
        });

        return new D3D11RasterState(rasterState);
    }
}
