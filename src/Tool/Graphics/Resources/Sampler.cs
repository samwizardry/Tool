// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Tool.Collections;

using Vortice.Mathematics;

namespace Tool.Graphics;

public readonly record struct SamplerDescription
{
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

    public readonly Color4 BorderColor { get; init; }
    public readonly float MaxAnisotropy { get; init; }
    public readonly float MipBias { get; init; }
    public readonly bool MinFilter { get; init; }
    public readonly bool MagFilter { get; init; }
    public readonly bool MipFilter { get; init; }
    public readonly SamplerAddressMode AddressU { get; init; }
    public readonly SamplerAddressMode AddressV { get; init; }
    public readonly SamplerAddressMode AddressW { get; init; }
    public readonly SamplerReductionType ReductionType { get; init; }

    public static SamplerDescription PointClamp => new(false, false, false,
        SamplerAddressMode.Clamp, SamplerAddressMode.Clamp, SamplerAddressMode.Clamp);

    public static SamplerDescription PointWrap => new(false, false, false,
        SamplerAddressMode.Wrap, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap);

    public static SamplerDescription LinearClamp => new(true, true, true,
        SamplerAddressMode.Clamp, SamplerAddressMode.Clamp, SamplerAddressMode.Clamp);

    /// <summary>
    /// Bilinear
    /// </summary>
    public static SamplerDescription Bilinear => new(true, true, false,
        SamplerAddressMode.Wrap, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap);

    /// <summary>
    /// Trilinear.
    /// </summary>
    public static SamplerDescription LinearWrap => new(true, true, true,
        SamplerAddressMode.Wrap, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap);

    /// <summary>
    /// Anisotropic.
    /// </summary>
    public static SamplerDescription AnisotropicWrap(float maxAnisotropy = 16.0f) =>
        new(true, true, true, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap, SamplerAddressMode.Wrap, maxAnisotropy);
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
