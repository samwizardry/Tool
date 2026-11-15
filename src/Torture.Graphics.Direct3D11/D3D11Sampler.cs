// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Vortice.Direct3D11;

using CoreGfx = Torture.Core.Graphics;

namespace Torture.Graphics.Direct3D11;

internal sealed class D3D11Sampler : CoreGfx.Sampler
{
    public D3D11Sampler(ID3D11Device5 device, in CoreGfx.SamplerDescription desc)
        : base(desc)
    {
        Native = device.CreateSamplerState(new SamplerDescription(
            filter: ConvertFilter(in desc),
            addressU: ConvertAddressMode(desc.AddressU),
            addressV: ConvertAddressMode(desc.AddressV),
            addressW: ConvertAddressMode(desc.AddressW),
            mipLODBias: desc.MipBias,
            maxAnisotropy: Math.Max(1u, (uint)desc.MaxAnisotropy),
            comparisonFunc: desc.ReductionType == CoreGfx.SamplerReductionType.Comparison
                ? ComparisonFunction.Less
                : ComparisonFunction.Never,
            borderColor: desc.BorderColor,
            minLOD: 0.0f,
            maxLOD: float.MaxValue));
    }

    public ID3D11SamplerState Native { get; }

    private static Filter ConvertFilter(in CoreGfx.SamplerDescription desc)
    {
        // Matches D3D11_ENCODE_BASIC_FILTER and D3D11_ENCODE_ANISOTROPIC_FILTER macros from d3d11.h
        uint reduction = (uint)desc.ReductionType & 0x3u;
        if (desc.MaxAnisotropy > 1.0f)
        {
            return (Filter)(0x55u | (reduction << 7));
        }
        uint min = desc.MinFilter ? 1u : 0u;
        uint mag = desc.MagFilter ? 1u : 0u;
        uint mip = desc.MipFilter ? 1u : 0u;
        return (Filter)((min << 4) | (mag << 2) | mip | (reduction << 7));
    }

    private static TextureAddressMode ConvertAddressMode(CoreGfx.SamplerAddressMode mode) =>
        mode switch
        {
            CoreGfx.SamplerAddressMode.Clamp => TextureAddressMode.Clamp,
            CoreGfx.SamplerAddressMode.Wrap => TextureAddressMode.Wrap,
            CoreGfx.SamplerAddressMode.Border => TextureAddressMode.Border,
            CoreGfx.SamplerAddressMode.Mirror => TextureAddressMode.Mirror,
            CoreGfx.SamplerAddressMode.MirrorOnce => TextureAddressMode.MirrorOnce,
            _ => TextureAddressMode.Clamp
        };

    public override void Dispose()
    {
        if (_disposed)
            return;

        Native?.Dispose();

        _disposed = true;
    }
}
