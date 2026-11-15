// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Vortice.Mathematics;

namespace Torture.Core.Graphics;

/// <summary>
/// Describes a texture sampler state modeled after nvrhi::SamplerDesc.
/// </summary>
public readonly record struct SamplerDescription
{
    public readonly Color4 BorderColor;
    public readonly float MaxAnisotropy;
    public readonly float MipBias;
    public readonly bool MinFilter;
    public readonly bool MagFilter;
    public readonly bool MipFilter;
    public readonly SamplerAddressMode AddressU;
    public readonly SamplerAddressMode AddressV;
    public readonly SamplerAddressMode AddressW;
    public readonly SamplerReductionType ReductionType;

    public SamplerDescription(
        bool minFilter = true,
        bool magFilter = true,
        bool mipFilter = true,
        SamplerAddressMode addressU = SamplerAddressMode.Clamp,
        SamplerAddressMode addressV = SamplerAddressMode.Clamp,
        SamplerAddressMode addressW = SamplerAddressMode.Clamp,
        float maxAnisotropy = 1.0f,
        float mipBias = 0.0f,
        SamplerReductionType reductionType = SamplerReductionType.Standard,
        Color4? borderColor = null)
    {
        MinFilter = minFilter;
        MagFilter = magFilter;
        MipFilter = mipFilter;
        AddressU = addressU;
        AddressV = addressV;
        AddressW = addressW;
        MaxAnisotropy = maxAnisotropy;
        MipBias = mipBias;
        ReductionType = reductionType;
        BorderColor = borderColor ?? new Color4(1.0f, 1.0f, 1.0f, 1.0f);
    }

    public static SamplerDescription PointClamp => new(false, false, false, SamplerAddressMode.Clamp, SamplerAddressMode.Clamp, SamplerAddressMode.Clamp);

    public static SamplerDescription PointWrap => new(false, false, false, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap);

    public static SamplerDescription LinearClamp => new(true, true, true, SamplerAddressMode.Clamp, SamplerAddressMode.Clamp, SamplerAddressMode.Clamp);

    public static SamplerDescription LinearWrap => new(true, true, true, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap);

    public static SamplerDescription AnisotropicWrap(float maxAnisotropy = 16.0f) =>
        new(true, true, true, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap, maxAnisotropy);
}