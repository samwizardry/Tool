// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Vortice.Direct3D11;

namespace Torture.Direct3D11;

internal sealed class D3D11Sampler : CoreSampler
{
    public D3D11Sampler(ID3D11Device5 device, in CoreSamplerDesc desc)
        : base(in desc)
    {
        Native = device.CreateSamplerState(new SamplerDescription(
            filter: ConvertFilter(in desc),
            addressU: ConvertAddressMode(desc.AddressU),
            addressV: ConvertAddressMode(desc.AddressV),
            addressW: ConvertAddressMode(desc.AddressW),
            mipLODBias: desc.MipBias,
            maxAnisotropy: System.Math.Max(1u, (uint)desc.MaxAnisotropy),
            comparisonFunc: desc.ReductionType == CoreSamplerReductionType.Comparison
                ? ComparisonFunction.Less
                : ComparisonFunction.Never,
            borderColor: desc.BorderColor,
            minLOD: 0.0f,
            maxLOD: float.MaxValue));
    }

    public ID3D11SamplerState Native { get; }

    private static Filter ConvertFilter(in CoreSamplerDesc desc)
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

    private static TextureAddressMode ConvertAddressMode(CoreSamplerAddressMode mode) =>
        mode switch
        {
            CoreSamplerAddressMode.Clamp => TextureAddressMode.Clamp,
            CoreSamplerAddressMode.Wrap => TextureAddressMode.Wrap,
            CoreSamplerAddressMode.Border => TextureAddressMode.Border,
            CoreSamplerAddressMode.Mirror => TextureAddressMode.Mirror,
            CoreSamplerAddressMode.MirrorOnce => TextureAddressMode.MirrorOnce,
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
