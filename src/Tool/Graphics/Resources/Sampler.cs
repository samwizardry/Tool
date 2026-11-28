// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Tool.Collections;

using Vortice.Mathematics;

namespace Tool.Graphics;

public readonly record struct SamplerDescription
{
    public readonly bool MinFilter;
    public readonly bool MagFilter;
    public readonly bool MipFilter;
    public readonly SamplerAddressMode AddressU;
    public readonly SamplerAddressMode AddressV;
    public readonly SamplerAddressMode AddressW;
    public readonly float MaxAnisotropy;
    public readonly float MipBias;
    public readonly SamplerReductionType ReductionType;
    public readonly Color4 BorderColor;

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
        in Color4? borderColor = null)
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
        BorderColor = borderColor ?? Colors.White;
    }

    public static SamplerDescription PointClamp => new SamplerDescription(false, false, false,
        SamplerAddressMode.Clamp, SamplerAddressMode.Clamp, SamplerAddressMode.Clamp);
    public static SamplerDescription PointWrap => new SamplerDescription(false, false, false,
        SamplerAddressMode.Wrap, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap);
    public static SamplerDescription LinearClamp => new SamplerDescription(true, true, true,
        SamplerAddressMode.Clamp, SamplerAddressMode.Clamp, SamplerAddressMode.Clamp);
    public static SamplerDescription LinearWrap => new SamplerDescription(true, true, true,
        SamplerAddressMode.Wrap, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap);

    public static SamplerDescription Anisotropic(
        float maxAnisotropy = 16.0f,
        SamplerAddressMode addressU = SamplerAddressMode.Wrap,
        SamplerAddressMode addressV = SamplerAddressMode.Wrap,
        SamplerAddressMode addressW = SamplerAddressMode.Wrap) =>
        new SamplerDescription(true, true, true, addressU, addressV, addressW, maxAnisotropy);
}

public readonly record struct SamplerHandle
{
    public readonly SlotHandle Raw;

    public SamplerHandle(SlotHandle raw)
    {
        Raw = raw;
    }

    public static SamplerHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
