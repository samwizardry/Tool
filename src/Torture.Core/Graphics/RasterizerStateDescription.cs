// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

namespace Torture.Core.Graphics;

public readonly record struct RasterizerStateDescription
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

    // Extended rasterizer state supported by Maxwell
    public readonly byte ForcedSampleCount = 0;
    public readonly bool ProgrammableSamplePositionsEnable = false;
    public readonly bool ConservativeRasterEnable = false;
    public readonly bool QuadFillEnable = false;
    private readonly byte[] _samplePositionsX = new byte[16];
    private readonly byte[] _samplePositionsY = new byte[16];

    public RasterizerStateDescription(
        RasterFillMode fillMode,
        RasterCullMode cullMode,
        bool frontCounterClockwise,
        bool depthClipEnable,
        bool scissorEnable,
        bool multisampleEnable,
        bool antialiasedLineEnable,
        int depthBias,
        float depthBiasClamp,
        float slopeScaledDepthBias,
        byte forcedSampleCount,
        bool programmableSamplePositionsEnable,
        bool conservativeRasterEnable,
        bool quadFillEnable,
        ReadOnlySpan<byte> samplePositionsX,
        ReadOnlySpan<byte> samplePositionsY)
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
        ProgrammableSamplePositionsEnable = programmableSamplePositionsEnable;
        ConservativeRasterEnable = conservativeRasterEnable;
        QuadFillEnable = quadFillEnable;

        for (int i = 0; i < samplePositionsX.Length; i++)
        {
            _samplePositionsX[i] = samplePositionsX[i];
        }

        for (int i = 0; i < samplePositionsY.Length; i++)
        {
            _samplePositionsY[i] = samplePositionsY[i];
        }
    }

    public ReadOnlySpan<byte> SamplePositionsX => _samplePositionsX;
    public ReadOnlySpan<byte> SamplePositionsY => _samplePositionsY;
}

public struct RasterizerStateDescriptionBuilder
{
    public RasterFillMode FillMode;
    public RasterCullMode CullMode;
    public bool FrontCounterClockwise;
    public bool DepthClipEnable;
    public bool ScissorEnable;
    public bool MultisampleEnable;
    public bool AntialiasedLineEnable;
    public int DepthBias;
    public float DepthBiasClamp;
    public float SlopeScaledDepthBias;

    // Extended rasterizer state supported by Maxwell
    public byte ForcedSampleCount = 0;
    public bool ProgrammableSamplePositionsEnable = false;
    public bool ConservativeRasterEnable = false;
    public bool QuadFillEnable = false;
    public byte[] SamplePositionsX = new byte[16];
    public byte[] SamplePositionsY = new byte[16];

    public RasterizerStateDescriptionBuilder() { }

    public static RasterizerStateDescriptionBuilder CullBack => new() { CullMode = RasterCullMode.Back, FillMode = RasterFillMode.Solid };
    public static RasterizerStateDescriptionBuilder CullFront => new() { CullMode = RasterCullMode.Front, FillMode = RasterFillMode.Solid };
    public static RasterizerStateDescriptionBuilder CullNone => new() { CullMode = RasterCullMode.None, FillMode = RasterFillMode.Solid };
    public static RasterizerStateDescriptionBuilder Wireframe => new() { CullMode = RasterCullMode.None, FillMode = RasterFillMode.Wireframe };

    public void SetSamplePositions(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        for (int i = 0; i < x.Length; i++)
        {
            SamplePositionsX[i] = x[i];
        }

        for (int i = 0; i < y.Length; i++)
        {
            SamplePositionsY[i] = y[i];
        }
    }

    public RasterizerStateDescription Build()
    {
        return new RasterizerStateDescription(
            FillMode,
            CullMode,
            FrontCounterClockwise,
            DepthClipEnable,
            ScissorEnable,
            MultisampleEnable,
            AntialiasedLineEnable,
            DepthBias,
            DepthBiasClamp,
            SlopeScaledDepthBias,
            ForcedSampleCount,
            ProgrammableSamplePositionsEnable,
            ConservativeRasterEnable,
            QuadFillEnable,
            SamplePositionsX,
            SamplePositionsY);
    }
}