// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Vortice.Direct3D;
using Vortice.DXGI;

namespace Torture.Direct3D11;

internal static class Extensions
{
    public readonly record struct DxgiFormatMapping(
        CoreFormat AbstractFormat,
        Format ResourceFormat,
        Format SrvFormat,
        Format RtvDsvFormat);

    private static readonly DxgiFormatMapping[] FormatMappings =
    [
        new(CoreFormat.Unknown,             Format.Unknown,                 Format.Unknown,                     Format.Unknown),
        new(CoreFormat.R8_UInt,             Format.R8_Typeless,             Format.R8_UInt,                     Format.R8_UInt),
        new(CoreFormat.R8_SInt,             Format.R8_Typeless,             Format.R8_SInt,                     Format.R8_SInt),
        new(CoreFormat.R8_UNorm,            Format.R8_Typeless,             Format.R8_UNorm,                    Format.R8_UNorm),
        new(CoreFormat.R8_SNorm,            Format.R8_Typeless,             Format.R8_SNorm,                    Format.R8_SNorm),
        new(CoreFormat.RG8_UInt,            Format.R8G8_Typeless,           Format.R8G8_UInt,                   Format.R8G8_UInt),
        new(CoreFormat.RG8_SInt,            Format.R8G8_Typeless,           Format.R8G8_SInt,                   Format.R8G8_SInt),
        new(CoreFormat.RG8_UNorm,           Format.R8G8_Typeless,           Format.R8G8_UNorm,                  Format.R8G8_UNorm),
        new(CoreFormat.RG8_SNorm,           Format.R8G8_Typeless,           Format.R8G8_SNorm,                  Format.R8G8_SNorm),
        new(CoreFormat.R16_UInt,            Format.R16_Typeless,            Format.R16_UInt,                    Format.R16_UInt),
        new(CoreFormat.R16_SInt,            Format.R16_Typeless,            Format.R16_SInt,                    Format.R16_SInt),
        new(CoreFormat.R16_UNorm,           Format.R16_Typeless,            Format.R16_UNorm,                   Format.R16_UNorm),
        new(CoreFormat.R16_SNorm,           Format.R16_Typeless,            Format.R16_SNorm,                   Format.R16_SNorm),
        new(CoreFormat.R16_Float,           Format.R16_Typeless,            Format.R16_Float,                   Format.R16_Float),
        new(CoreFormat.BGRA4_UNorm,         Format.B4G4R4A4_UNorm,          Format.B4G4R4A4_UNorm,              Format.B4G4R4A4_UNorm),
        new(CoreFormat.B5G6R5_UNorm,        Format.B5G6R5_UNorm,            Format.B5G6R5_UNorm,                Format.B5G6R5_UNorm),
        new(CoreFormat.B5G5R5A1_UNorm,      Format.B5G5R5A1_UNorm,          Format.B5G5R5A1_UNorm,              Format.B5G5R5A1_UNorm),
        new(CoreFormat.RGBA8_UInt,          Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_UInt,               Format.R8G8B8A8_UInt),
        new(CoreFormat.RGBA8_SInt,          Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_SInt,               Format.R8G8B8A8_SInt),
        new(CoreFormat.RGBA8_UNorm,         Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_UNorm,              Format.R8G8B8A8_UNorm),
        new(CoreFormat.RGBA8_SNorm,         Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_SNorm,              Format.R8G8B8A8_SNorm),
        new(CoreFormat.BGRA8_UNorm,         Format.B8G8R8A8_Typeless,       Format.B8G8R8A8_UNorm,              Format.B8G8R8A8_UNorm),
        new(CoreFormat.SRGBA8_UNorm,        Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_UNorm_SRgb,         Format.R8G8B8A8_UNorm_SRgb),
        new(CoreFormat.SBGRA8_UNorm,        Format.B8G8R8A8_Typeless,       Format.B8G8R8A8_UNorm_SRgb,         Format.B8G8R8A8_UNorm_SRgb),
        new(CoreFormat.R10G10B10A2_UNorm,   Format.R10G10B10A2_Typeless,    Format.R10G10B10A2_UNorm,           Format.R10G10B10A2_UNorm),
        new(CoreFormat.R11G11B10_Float,     Format.R11G11B10_Float,         Format.R11G11B10_Float,             Format.R11G11B10_Float),
        new(CoreFormat.RG16_UInt,           Format.R16G16_Typeless,         Format.R16G16_UInt,                 Format.R16G16_UInt),
        new(CoreFormat.RG16_SInt,           Format.R16G16_Typeless,         Format.R16G16_SInt,                 Format.R16G16_SInt),
        new(CoreFormat.RG16_UNorm,          Format.R16G16_Typeless,         Format.R16G16_UNorm,                Format.R16G16_UNorm),
        new(CoreFormat.RG16_SNorm,          Format.R16G16_Typeless,         Format.R16G16_SNorm,                Format.R16G16_SNorm),
        new(CoreFormat.RG16_Float,          Format.R16G16_Typeless,         Format.R16G16_Float,                Format.R16G16_Float),
        new(CoreFormat.R32_UInt,            Format.R32_Typeless,            Format.R32_UInt,                    Format.R32_UInt),
        new(CoreFormat.R32_SInt,            Format.R32_Typeless,            Format.R32_SInt,                    Format.R32_SInt),
        new(CoreFormat.R32_Float,           Format.R32_Typeless,            Format.R32_Float,                   Format.R32_Float),
        new(CoreFormat.RGBA16_UInt,         Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_UInt,           Format.R16G16B16A16_UInt),
        new(CoreFormat.RGBA16_SInt,         Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_SInt,           Format.R16G16B16A16_SInt),
        new(CoreFormat.RGBA16_Float,        Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_Float,          Format.R16G16B16A16_Float),
        new(CoreFormat.RGBA16_UNorm,        Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_UNorm,          Format.R16G16B16A16_UNorm),
        new(CoreFormat.RGBA16_SNorm,        Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_SNorm,          Format.R16G16B16A16_SNorm),
        new(CoreFormat.RG32_UInt,           Format.R32G32_Typeless,         Format.R32G32_UInt,                 Format.R32G32_UInt),
        new(CoreFormat.RG32_SInt,           Format.R32G32_Typeless,         Format.R32G32_SInt,                 Format.R32G32_SInt),
        new(CoreFormat.RG32_Float,          Format.R32G32_Typeless,         Format.R32G32_Float,                Format.R32G32_Float),
        new(CoreFormat.RGB32_UInt,          Format.R32G32B32_Typeless,      Format.R32G32B32_UInt,              Format.R32G32B32_UInt),
        new(CoreFormat.RGB32_SInt,          Format.R32G32B32_Typeless,      Format.R32G32B32_SInt,              Format.R32G32B32_SInt),
        new(CoreFormat.RGB32_Float,         Format.R32G32B32_Typeless,      Format.R32G32B32_Float,             Format.R32G32B32_Float),
        new(CoreFormat.RGBA32_UInt,         Format.R32G32B32A32_Typeless,   Format.R32G32B32A32_UInt,           Format.R32G32B32A32_UInt),
        new(CoreFormat.RGBA32_SInt,         Format.R32G32B32A32_Typeless,   Format.R32G32B32A32_SInt,           Format.R32G32B32A32_SInt),
        new(CoreFormat.RGBA32_Float,        Format.R32G32B32A32_Typeless,   Format.R32G32B32A32_Float,          Format.R32G32B32A32_Float),
        new(CoreFormat.D16,                 Format.R16_Typeless,            Format.R16_UNorm,                   Format.D16_UNorm),
        new(CoreFormat.D24S8,               Format.R24G8_Typeless,          Format.R24_UNorm_X8_Typeless,       Format.D24_UNorm_S8_UInt),
        new(CoreFormat.X24G8_UInt,          Format.R24G8_Typeless,          Format.X24_Typeless_G8_UInt,        Format.D24_UNorm_S8_UInt),
        new(CoreFormat.D32,                 Format.R32_Typeless,            Format.R32_Float,                   Format.D32_Float),
        new(CoreFormat.D32S8,               Format.R32G8X24_Typeless,       Format.R32_Float_X8X24_Typeless,    Format.D32_Float_S8X24_UInt),
        new(CoreFormat.X32G8_UInt,          Format.R32G8X24_Typeless,       Format.X32_Typeless_G8X24_UInt,     Format.D32_Float_S8X24_UInt),
        new(CoreFormat.BC1_UNorm,           Format.BC1_Typeless,            Format.BC1_UNorm,                   Format.BC1_UNorm),
        new(CoreFormat.BC1_UNorm_Srgb,      Format.BC1_Typeless,            Format.BC1_UNorm_SRgb,              Format.BC1_UNorm_SRgb),
        new(CoreFormat.BC2_UNorm,           Format.BC2_Typeless,            Format.BC2_UNorm,                   Format.BC2_UNorm),
        new(CoreFormat.BC2_UNorm_Srgb,      Format.BC2_Typeless,            Format.BC2_UNorm_SRgb,              Format.BC2_UNorm_SRgb),
        new(CoreFormat.BC3_UNorm,           Format.BC3_Typeless,            Format.BC3_UNorm,                   Format.BC3_UNorm),
        new(CoreFormat.BC3_UNorm_Srgb,      Format.BC3_Typeless,            Format.BC3_UNorm_SRgb,              Format.BC3_UNorm_SRgb),
        new(CoreFormat.BC4_UNorm,           Format.BC4_Typeless,            Format.BC4_UNorm,                   Format.BC4_UNorm),
        new(CoreFormat.BC4_SNorm,           Format.BC4_Typeless,            Format.BC4_SNorm,                   Format.BC4_SNorm),
        new(CoreFormat.BC5_UNorm,           Format.BC5_Typeless,            Format.BC5_UNorm,                   Format.BC5_UNorm),
        new(CoreFormat.BC5_SNorm,           Format.BC5_Typeless,            Format.BC5_SNorm,                   Format.BC5_SNorm),
        new(CoreFormat.BC6H_UFloat,         Format.BC6H_Typeless,           Format.BC6H_Uf16,                   Format.BC6H_Uf16),
        new(CoreFormat.BC6H_SFloat,         Format.BC6H_Typeless,           Format.BC6H_Sf16,                   Format.BC6H_Sf16),
        new(CoreFormat.BC7_UNorm,           Format.BC7_Typeless,            Format.BC7_UNorm,                   Format.BC7_UNorm),
        new(CoreFormat.BC7_UNorm_Srgb,      Format.BC7_Typeless,            Format.BC7_UNorm_SRgb,              Format.BC7_UNorm_SRgb)
    ];

    public static ref readonly DxgiFormatMapping GetDxgiFormatMapping(this CoreFormat format) =>
        ref FormatMappings[(int)format];

    public static Format ToDXGIFormat(this CoreFormat format) =>
        FormatMappings[(int)format].SrvFormat;

    public static Format ToDXGIFormat(this CoreIndexFormat format) =>
        format switch
        {
            CoreIndexFormat.UInt16 => Format.R16_UInt,
            CoreIndexFormat.UInt32 => Format.R32_UInt,
            _ => throw new NotSupportedException()
        };

    public static Format ToDXGIFormat(this CoreVertexElementFormat format) =>
        format switch
        {
            CoreVertexElementFormat.Float1 => Format.R32_Float,
            CoreVertexElementFormat.Float2 => Format.R32G32_Float,
            CoreVertexElementFormat.Float3 => Format.R32G32B32_Float,
            CoreVertexElementFormat.Float4 => Format.R32G32B32A32_Float,
            CoreVertexElementFormat.Int1 => Format.R32_SInt,
            CoreVertexElementFormat.Int2 => Format.R32G32_SInt,
            CoreVertexElementFormat.Int3 => Format.R32G32B32_SInt,
            CoreVertexElementFormat.Int4 => Format.R32G32B32A32_SInt,
            CoreVertexElementFormat.UInt1 => Format.R32_UInt,
            CoreVertexElementFormat.UInt2 => Format.R32G32_UInt,
            CoreVertexElementFormat.UInt3 => Format.R32G32B32_UInt,
            CoreVertexElementFormat.UInt4 => Format.R32G32B32A32_UInt,
            CoreVertexElementFormat.Half2 => Format.R16G16_Float,
            CoreVertexElementFormat.Half4 => Format.R16G16B16A16_Float,
            CoreVertexElementFormat.Short2 => Format.R16G16_SInt,
            CoreVertexElementFormat.Short4 => Format.R16G16B16A16_SInt,
            CoreVertexElementFormat.UShort2 => Format.R16G16_UInt,
            CoreVertexElementFormat.UShort4 => Format.R16G16B16A16_UInt,
            CoreVertexElementFormat.NormShort2 => Format.R16G16_SNorm,
            CoreVertexElementFormat.NormShort4 => Format.R16G16B16A16_SNorm,
            CoreVertexElementFormat.UNormShort2 => Format.R16G16_UNorm,
            CoreVertexElementFormat.UNormShort4 => Format.R16G16B16A16_UNorm,
            CoreVertexElementFormat.Byte2 => Format.R8G8_SInt,
            CoreVertexElementFormat.Byte4 => Format.R8G8B8A8_SInt,
            CoreVertexElementFormat.UByte2 => Format.R8G8_UInt,
            CoreVertexElementFormat.UByte4 => Format.R8G8B8A8_UInt,
            CoreVertexElementFormat.NormByte2 => Format.R8G8_SNorm,
            CoreVertexElementFormat.NormByte4 => Format.R8G8B8A8_SNorm,
            CoreVertexElementFormat.UNormByte2 => Format.R8G8_UNorm,
            CoreVertexElementFormat.UNormByte4 => Format.R8G8B8A8_UNorm,
            _ => throw new NotSupportedException()
        };

    public static string ToD3D11SemanticName(this CoreVertexElementSemantic semantic) =>
        semantic switch
        {
            CoreVertexElementSemantic.Position => "POSITION",
            CoreVertexElementSemantic.Color => "COLOR",
            CoreVertexElementSemantic.TexCoord => "TEXCOORD",
            CoreVertexElementSemantic.Normal => "NORMAL",
            CoreVertexElementSemantic.Tangent => "TANGENT",
            CoreVertexElementSemantic.BlendIndices => "BLENDINDICES",
            CoreVertexElementSemantic.BlendWeight => "BLENDWEIGHT",
            _ => throw new NotSupportedException()
        };

    public static PrimitiveTopology ToD3D11PrimitiveTopology(this CorePrimitiveTopology topology) =>
        topology switch
        {
            CorePrimitiveTopology.Undefined => PrimitiveTopology.Undefined,
            CorePrimitiveTopology.PointList => PrimitiveTopology.PointList,
            CorePrimitiveTopology.LineList => PrimitiveTopology.LineList,
            CorePrimitiveTopology.LineStrip => PrimitiveTopology.LineStrip,
            CorePrimitiveTopology.TriangleList => PrimitiveTopology.TriangleList,
            CorePrimitiveTopology.TriangleStrip => PrimitiveTopology.TriangleStrip,
            CorePrimitiveTopology.TriangleFan => PrimitiveTopology.TriangleFan,
            CorePrimitiveTopology.TriangleListWithAdjacency => PrimitiveTopology.TriangleListAdjacency,
            CorePrimitiveTopology.TriangleStripWithAdjacency => PrimitiveTopology.TriangleStripAdjacency,
            _ => throw new NotSupportedException()
        };
}
