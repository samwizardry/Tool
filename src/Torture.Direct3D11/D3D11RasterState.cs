// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Vortice.Direct3D11;

namespace Torture.Direct3D11;

internal sealed class D3D11RasterState : CoreRasterState
{
    public D3D11RasterState(ID3D11Device5 device, in CoreRasterStateDesc desc)
        : base(in desc)
    {
        Native = device.CreateRasterizerState2(new()
        {
            FillMode = desc.FillMode switch
            {
                CoreRasterFillMode.Solid => FillMode.Solid,
                CoreRasterFillMode.Wireframe => FillMode.Wireframe,
                _ => throw new NotSupportedException()
            },
            CullMode = desc.CullMode switch
            {
                CoreRasterCullMode.Back => CullMode.Back,
                CoreRasterCullMode.Front => CullMode.Front,
                CoreRasterCullMode.None => CullMode.None,
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
    }

    public ID3D11RasterizerState Native { get; }

    protected override void FreeUnmanagedResources()
    {
        Native?.Dispose();
    }
}
