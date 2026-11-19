// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Vortice.Direct3D11;

using TortGfx = Torture.Graphics;

namespace Torture.Direct3D11;

internal sealed class D3D11RasterState : TortGfx.RasterState
{
    public D3D11RasterState(ID3D11Device5 device, in TortGfx.RasterStateDescription desc)
        : base(in desc)
    {
        Native = device.CreateRasterizerState2(new()
        {
            FillMode = desc.FillMode switch
            {
                TortGfx.RasterFillMode.Solid => FillMode.Solid,
                TortGfx.RasterFillMode.Wireframe => FillMode.Wireframe,
                _ => throw new NotSupportedException()
            },
            CullMode = desc.CullMode switch
            {
                TortGfx.RasterCullMode.Back => CullMode.Back,
                TortGfx.RasterCullMode.Front => CullMode.Front,
                TortGfx.RasterCullMode.None => CullMode.None,
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

    public override void Dispose()
    {
        if (_disposed)
            return;

        Native.Dispose();

        _disposed = true;
    }
}
