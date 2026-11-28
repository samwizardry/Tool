// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Tool.Collections;

namespace Tool.Graphics;

public readonly record struct RasterStateDescription
{
    public readonly RasterFillMode FillMode;
    public readonly RasterCullMode CullMode;
    public readonly bool FrontCounterClockwise;
    public readonly bool DepthClipEnable;
    public readonly bool ScissorEnable;
    public readonly bool MultisampleEnable;
    public readonly bool AntialiasedLineEnable;
    public readonly int DepthBias;
    public readonly float DepthBiasClamp;
    public readonly float SlopeScaledDepthBias;
    public readonly byte ForcedSampleCount;
    public readonly bool ConservativeRasterEnable;

    public RasterStateDescription(
        RasterFillMode fillMode = RasterFillMode.Solid,
        RasterCullMode cullMode = RasterCullMode.Back,
        bool frontCounterClockwise = true,
        bool depthClipEnable = true,
        bool scissorEnable = false,
        bool multisampleEnable = false,
        bool antialiasedLineEnable = false,
        int depthBias = 0,
        float depthBiasClamp = 0.0f,
        float slopeScaledDepthBias = 0.0f,
        byte forcedSampleCount = 0,
        bool conservativeRasterEnable = false)
    {
        FillMode = fillMode;
        CullMode = cullMode;
        FrontCounterClockwise = frontCounterClockwise;
        DepthClipEnable = depthClipEnable;
        ScissorEnable = scissorEnable;
        MultisampleEnable = multisampleEnable;
        AntialiasedLineEnable = antialiasedLineEnable;
        DepthBias = depthBias;
        DepthBiasClamp = depthBiasClamp;
        SlopeScaledDepthBias = slopeScaledDepthBias;
        ForcedSampleCount = forcedSampleCount;
        ConservativeRasterEnable = conservativeRasterEnable;
    }

    public static RasterStateDescription CullBack => new RasterStateDescription(fillMode: RasterFillMode.Solid, cullMode: RasterCullMode.Back);
    public static RasterStateDescription CullFront => new RasterStateDescription(fillMode: RasterFillMode.Solid, cullMode: RasterCullMode.Front);
    public static RasterStateDescription CullNone => new RasterStateDescription(fillMode: RasterFillMode.Solid, cullMode: RasterCullMode.None);
    public static RasterStateDescription Wireframe => new RasterStateDescription(fillMode: RasterFillMode.Wireframe, cullMode: RasterCullMode.None);
}

public readonly record struct RasterStateHandle
{
    public readonly SlotHandle Raw;

    public RasterStateHandle(SlotHandle raw)
    {
        Raw = raw;
    }

    public static RasterStateHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
