// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

namespace Tool.Graphics;

/// <summary>
/// Provides size and block layout metadata for a <see cref="Format"/>.
/// </summary>
public readonly record struct FormatInfo
{
    private static readonly FormatInfo[] Table =
    [
        new(Format.Unknown,           0,  0, false, false),
        new(Format.R8_UInt,           1,  1, false, false),
        new(Format.R8_SInt,           1,  1, false, false),
        new(Format.R8_UNorm,          1,  1, false, false),
        new(Format.R8_SNorm,          1,  1, false, false),
        new(Format.RG8_UInt,          2,  1, false, false),
        new(Format.RG8_SInt,          2,  1, false, false),
        new(Format.RG8_UNorm,         2,  1, false, false),
        new(Format.RG8_SNorm,         2,  1, false, false),
        new(Format.R16_UInt,          2,  1, false, false),
        new(Format.R16_SInt,          2,  1, false, false),
        new(Format.R16_UNorm,         2,  1, false, false),
        new(Format.R16_SNorm,         2,  1, false, false),
        new(Format.R16_Float,         2,  1, false, false),
        new(Format.BGRA4_UNorm,       2,  1, false, false),
        new(Format.B5G6R5_UNorm,      2,  1, false, false),
        new(Format.B5G5R5A1_UNorm,    2,  1, false, false),
        new(Format.RGBA8_UInt,        4,  1, false, false),
        new(Format.RGBA8_SInt,        4,  1, false, false),
        new(Format.RGBA8_UNorm,       4,  1, false, false),
        new(Format.RGBA8_SNorm,       4,  1, false, false),
        new(Format.BGRA8_UNorm,       4,  1, false, false),
        new(Format.SRGBA8_UNorm,      4,  1, false, false),
        new(Format.SBGRA8_UNorm,      4,  1, false, false),
        new(Format.R10G10B10A2_UNorm, 4,  1, false, false),
        new(Format.R11G11B10_Float,   4,  1, false, false),
        new(Format.RG16_UInt,         4,  1, false, false),
        new(Format.RG16_SInt,         4,  1, false, false),
        new(Format.RG16_UNorm,        4,  1, false, false),
        new(Format.RG16_SNorm,        4,  1, false, false),
        new(Format.RG16_Float,        4,  1, false, false),
        new(Format.R32_UInt,          4,  1, false, false),
        new(Format.R32_SInt,          4,  1, false, false),
        new(Format.R32_Float,         4,  1, false, false),
        new(Format.RGBA16_UInt,       8,  1, false, false),
        new(Format.RGBA16_SInt,       8,  1, false, false),
        new(Format.RGBA16_Float,      8,  1, false, false),
        new(Format.RGBA16_UNorm,      8,  1, false, false),
        new(Format.RGBA16_SNorm,      8,  1, false, false),
        new(Format.RG32_UInt,         8,  1, false, false),
        new(Format.RG32_SInt,         8,  1, false, false),
        new(Format.RG32_Float,        8,  1, false, false),
        new(Format.RGB32_UInt,        12, 1, false, false),
        new(Format.RGB32_SInt,        12, 1, false, false),
        new(Format.RGB32_Float,       12, 1, false, false),
        new(Format.RGBA32_UInt,       16, 1, false, false),
        new(Format.RGBA32_SInt,       16, 1, false, false),
        new(Format.RGBA32_Float,      16, 1, false, false),
        new(Format.D16,               2,  1, true,  false),
        new(Format.D24S8,             4,  1, true,  true),
        new(Format.X24G8_UInt,        4,  1, false, true),
        new(Format.D32,               4,  1, true,  false),
        new(Format.D32S8,             8,  1, true,  true),
        new(Format.X32G8_UInt,        8,  1, false, true),
        new(Format.BC1_UNorm,         8,  4, false, false),
        new(Format.BC1_UNorm_Srgb,    8,  4, false, false),
        new(Format.BC2_UNorm,         16, 4, false, false),
        new(Format.BC2_UNorm_Srgb,    16, 4, false, false),
        new(Format.BC3_UNorm,         16, 4, false, false),
        new(Format.BC3_UNorm_Srgb,    16, 4, false, false),
        new(Format.BC4_UNorm,         8,  4, false, false),
        new(Format.BC4_SNorm,         8,  4, false, false),
        new(Format.BC5_UNorm,         16, 4, false, false),
        new(Format.BC5_SNorm,         16, 4, false, false),
        new(Format.BC6H_UFloat,       16, 4, false, false),
        new(Format.BC6H_SFloat,       16, 4, false, false),
        new(Format.BC7_UNorm,         16, 4, false, false),
        new(Format.BC7_UNorm_Srgb,    16, 4, false, false),
    ];

    public readonly Format Format;
    public readonly byte BytesPerBlock;
    public readonly byte BlockSize;
    public readonly bool IsDepth;
    public readonly bool IsStencil;

    public FormatInfo(
        Format format,
        byte bytesPerBlock,
        byte blockSize,
        bool isDepth,
        bool isStencil)
    {
        Format = format;
        BytesPerBlock = bytesPerBlock;
        BlockSize = blockSize;
        IsDepth = isDepth;
        IsStencil = isStencil;
    }

    public static FormatInfo Get(Format format) => Table[(int)format];

    public static void GetSurfaceInfo(Format format, uint width, uint height, out uint rowPitch, out uint slicePitch)
    {
        FormatInfo info = Table[(int)format];
        if (info.BlockSize == 0)
        {
            rowPitch = 0;
            slicePitch = 0;
            return;
        }

        uint numBlocksWide = Math.Max(1u, (width + info.BlockSize - 1u) / info.BlockSize);
        uint numBlocksHigh = Math.Max(1u, (height + info.BlockSize - 1u) / info.BlockSize);

        rowPitch = numBlocksWide * info.BytesPerBlock;
        slicePitch = rowPitch * numBlocksHigh;
    }
}
