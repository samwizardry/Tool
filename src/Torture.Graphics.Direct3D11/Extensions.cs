// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Vortice.Direct3D;
using Vortice.DXGI;

using CoreGfx = Torture.Core.Graphics;

namespace Torture.Graphics.Direct3D11;

public static class Extensions
{
    public readonly record struct DxgiFormatMapping(
        CoreGfx.Format AbstractFormat,
        Format ResourceFormat,
        Format SrvFormat,
        Format RtvDsvFormat);

    private static readonly DxgiFormatMapping[] FormatMappings =
    [
        new(CoreGfx.Format.Unknown,             Format.Unknown,                 Format.Unknown,                     Format.Unknown),
        new(CoreGfx.Format.R8_UInt,             Format.R8_Typeless,             Format.R8_UInt,                     Format.R8_UInt),
        new(CoreGfx.Format.R8_SInt,             Format.R8_Typeless,             Format.R8_SInt,                     Format.R8_SInt),
        new(CoreGfx.Format.R8_UNorm,            Format.R8_Typeless,             Format.R8_UNorm,                    Format.R8_UNorm),
        new(CoreGfx.Format.R8_SNorm,            Format.R8_Typeless,             Format.R8_SNorm,                    Format.R8_SNorm),
        new(CoreGfx.Format.RG8_UInt,            Format.R8G8_Typeless,           Format.R8G8_UInt,                   Format.R8G8_UInt),
        new(CoreGfx.Format.RG8_SInt,            Format.R8G8_Typeless,           Format.R8G8_SInt,                   Format.R8G8_SInt),
        new(CoreGfx.Format.RG8_UNorm,           Format.R8G8_Typeless,           Format.R8G8_UNorm,                  Format.R8G8_UNorm),
        new(CoreGfx.Format.RG8_SNorm,           Format.R8G8_Typeless,           Format.R8G8_SNorm,                  Format.R8G8_SNorm),
        new(CoreGfx.Format.R16_UInt,            Format.R16_Typeless,            Format.R16_UInt,                    Format.R16_UInt),
        new(CoreGfx.Format.R16_SInt,            Format.R16_Typeless,            Format.R16_SInt,                    Format.R16_SInt),
        new(CoreGfx.Format.R16_UNorm,           Format.R16_Typeless,            Format.R16_UNorm,                   Format.R16_UNorm),
        new(CoreGfx.Format.R16_SNorm,           Format.R16_Typeless,            Format.R16_SNorm,                   Format.R16_SNorm),
        new(CoreGfx.Format.R16_Float,           Format.R16_Typeless,            Format.R16_Float,                   Format.R16_Float),
        new(CoreGfx.Format.BGRA4_UNorm,         Format.B4G4R4A4_UNorm,          Format.B4G4R4A4_UNorm,              Format.B4G4R4A4_UNorm),
        new(CoreGfx.Format.B5G6R5_UNorm,        Format.B5G6R5_UNorm,            Format.B5G6R5_UNorm,                Format.B5G6R5_UNorm),
        new(CoreGfx.Format.B5G5R5A1_UNorm,      Format.B5G5R5A1_UNorm,          Format.B5G5R5A1_UNorm,              Format.B5G5R5A1_UNorm),
        new(CoreGfx.Format.RGBA8_UInt,          Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_UInt,               Format.R8G8B8A8_UInt),
        new(CoreGfx.Format.RGBA8_SInt,          Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_SInt,               Format.R8G8B8A8_SInt),
        new(CoreGfx.Format.RGBA8_UNorm,         Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_UNorm,              Format.R8G8B8A8_UNorm),
        new(CoreGfx.Format.RGBA8_SNorm,         Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_SNorm,              Format.R8G8B8A8_SNorm),
        new(CoreGfx.Format.BGRA8_UNorm,         Format.B8G8R8A8_Typeless,       Format.B8G8R8A8_UNorm,              Format.B8G8R8A8_UNorm),
        new(CoreGfx.Format.SRGBA8_UNorm,        Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_UNorm_SRgb,         Format.R8G8B8A8_UNorm_SRgb),
        new(CoreGfx.Format.SBGRA8_UNorm,        Format.B8G8R8A8_Typeless,       Format.B8G8R8A8_UNorm_SRgb,         Format.B8G8R8A8_UNorm_SRgb),
        new(CoreGfx.Format.R10G10B10A2_UNorm,   Format.R10G10B10A2_Typeless,    Format.R10G10B10A2_UNorm,           Format.R10G10B10A2_UNorm),
        new(CoreGfx.Format.R11G11B10_Float,     Format.R11G11B10_Float,         Format.R11G11B10_Float,             Format.R11G11B10_Float),
        new(CoreGfx.Format.RG16_UInt,           Format.R16G16_Typeless,         Format.R16G16_UInt,                 Format.R16G16_UInt),
        new(CoreGfx.Format.RG16_SInt,           Format.R16G16_Typeless,         Format.R16G16_SInt,                 Format.R16G16_SInt),
        new(CoreGfx.Format.RG16_UNorm,          Format.R16G16_Typeless,         Format.R16G16_UNorm,                Format.R16G16_UNorm),
        new(CoreGfx.Format.RG16_SNorm,          Format.R16G16_Typeless,         Format.R16G16_SNorm,                Format.R16G16_SNorm),
        new(CoreGfx.Format.RG16_Float,          Format.R16G16_Typeless,         Format.R16G16_Float,                Format.R16G16_Float),
        new(CoreGfx.Format.R32_UInt,            Format.R32_Typeless,            Format.R32_UInt,                    Format.R32_UInt),
        new(CoreGfx.Format.R32_SInt,            Format.R32_Typeless,            Format.R32_SInt,                    Format.R32_SInt),
        new(CoreGfx.Format.R32_Float,           Format.R32_Typeless,            Format.R32_Float,                   Format.R32_Float),
        new(CoreGfx.Format.RGBA16_UInt,         Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_UInt,           Format.R16G16B16A16_UInt),
        new(CoreGfx.Format.RGBA16_SInt,         Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_SInt,           Format.R16G16B16A16_SInt),
        new(CoreGfx.Format.RGBA16_Float,        Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_Float,          Format.R16G16B16A16_Float),
        new(CoreGfx.Format.RGBA16_UNorm,        Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_UNorm,          Format.R16G16B16A16_UNorm),
        new(CoreGfx.Format.RGBA16_SNorm,        Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_SNorm,          Format.R16G16B16A16_SNorm),
        new(CoreGfx.Format.RG32_UInt,           Format.R32G32_Typeless,         Format.R32G32_UInt,                 Format.R32G32_UInt),
        new(CoreGfx.Format.RG32_SInt,           Format.R32G32_Typeless,         Format.R32G32_SInt,                 Format.R32G32_SInt),
        new(CoreGfx.Format.RG32_Float,          Format.R32G32_Typeless,         Format.R32G32_Float,                Format.R32G32_Float),
        new(CoreGfx.Format.RGB32_UInt,          Format.R32G32B32_Typeless,      Format.R32G32B32_UInt,              Format.R32G32B32_UInt),
        new(CoreGfx.Format.RGB32_SInt,          Format.R32G32B32_Typeless,      Format.R32G32B32_SInt,              Format.R32G32B32_SInt),
        new(CoreGfx.Format.RGB32_Float,         Format.R32G32B32_Typeless,      Format.R32G32B32_Float,             Format.R32G32B32_Float),
        new(CoreGfx.Format.RGBA32_UInt,         Format.R32G32B32A32_Typeless,   Format.R32G32B32A32_UInt,           Format.R32G32B32A32_UInt),
        new(CoreGfx.Format.RGBA32_SInt,         Format.R32G32B32A32_Typeless,   Format.R32G32B32A32_SInt,           Format.R32G32B32A32_SInt),
        new(CoreGfx.Format.RGBA32_Float,        Format.R32G32B32A32_Typeless,   Format.R32G32B32A32_Float,          Format.R32G32B32A32_Float),
        new(CoreGfx.Format.D16,                 Format.R16_Typeless,            Format.R16_UNorm,                   Format.D16_UNorm),
        new(CoreGfx.Format.D24S8,               Format.R24G8_Typeless,          Format.R24_UNorm_X8_Typeless,       Format.D24_UNorm_S8_UInt),
        new(CoreGfx.Format.X24G8_UInt,          Format.R24G8_Typeless,          Format.X24_Typeless_G8_UInt,        Format.D24_UNorm_S8_UInt),
        new(CoreGfx.Format.D32,                 Format.R32_Typeless,            Format.R32_Float,                   Format.D32_Float),
        new(CoreGfx.Format.D32S8,               Format.R32G8X24_Typeless,       Format.R32_Float_X8X24_Typeless,    Format.D32_Float_S8X24_UInt),
        new(CoreGfx.Format.X32G8_UInt,          Format.R32G8X24_Typeless,       Format.X32_Typeless_G8X24_UInt,     Format.D32_Float_S8X24_UInt),
        new(CoreGfx.Format.BC1_UNorm,           Format.BC1_Typeless,            Format.BC1_UNorm,                   Format.BC1_UNorm),
        new(CoreGfx.Format.BC1_UNorm_Srgb,      Format.BC1_Typeless,            Format.BC1_UNorm_SRgb,              Format.BC1_UNorm_SRgb),
        new(CoreGfx.Format.BC2_UNorm,           Format.BC2_Typeless,            Format.BC2_UNorm,                   Format.BC2_UNorm),
        new(CoreGfx.Format.BC2_UNorm_Srgb,      Format.BC2_Typeless,            Format.BC2_UNorm_SRgb,              Format.BC2_UNorm_SRgb),
        new(CoreGfx.Format.BC3_UNorm,           Format.BC3_Typeless,            Format.BC3_UNorm,                   Format.BC3_UNorm),
        new(CoreGfx.Format.BC3_UNorm_Srgb,      Format.BC3_Typeless,            Format.BC3_UNorm_SRgb,              Format.BC3_UNorm_SRgb),
        new(CoreGfx.Format.BC4_UNorm,           Format.BC4_Typeless,            Format.BC4_UNorm,                   Format.BC4_UNorm),
        new(CoreGfx.Format.BC4_SNorm,           Format.BC4_Typeless,            Format.BC4_SNorm,                   Format.BC4_SNorm),
        new(CoreGfx.Format.BC5_UNorm,           Format.BC5_Typeless,            Format.BC5_UNorm,                   Format.BC5_UNorm),
        new(CoreGfx.Format.BC5_SNorm,           Format.BC5_Typeless,            Format.BC5_SNorm,                   Format.BC5_SNorm),
        new(CoreGfx.Format.BC6H_UFloat,         Format.BC6H_Typeless,           Format.BC6H_Uf16,                   Format.BC6H_Uf16),
        new(CoreGfx.Format.BC6H_SFloat,         Format.BC6H_Typeless,           Format.BC6H_Sf16,                   Format.BC6H_Sf16),
        new(CoreGfx.Format.BC7_UNorm,           Format.BC7_Typeless,            Format.BC7_UNorm,                   Format.BC7_UNorm),
        new(CoreGfx.Format.BC7_UNorm_Srgb,      Format.BC7_Typeless,            Format.BC7_UNorm_SRgb,              Format.BC7_UNorm_SRgb)
    ];

    public static ref readonly DxgiFormatMapping GetDxgiFormatMapping(this CoreGfx.Format format) =>
        ref FormatMappings[(int)format];

    public static Format ToDXGIFormat(this CoreGfx.Format format) =>
        FormatMappings[(int)format].SrvFormat;

    public static Format ToDXGIFormat(this CoreGfx.IndexFormat format) =>
        format switch
        {
            CoreGfx.IndexFormat.UInt16 => Format.R16_UInt,
            CoreGfx.IndexFormat.UInt32 => Format.R32_UInt,
            _ => throw new NotSupportedException()
        };

    public static Format ToDXGIFormat(this CoreGfx.VertexElementFormat format) =>
        format switch
        {
            CoreGfx.VertexElementFormat.Float1 => Format.R32_Float,
            CoreGfx.VertexElementFormat.Float2 => Format.R32G32_Float,
            CoreGfx.VertexElementFormat.Float3 => Format.R32G32B32_Float,
            CoreGfx.VertexElementFormat.Float4 => Format.R32G32B32A32_Float,
            CoreGfx.VertexElementFormat.Int1 => Format.R32_SInt,
            CoreGfx.VertexElementFormat.Int2 => Format.R32G32_SInt,
            CoreGfx.VertexElementFormat.Int3 => Format.R32G32B32_SInt,
            CoreGfx.VertexElementFormat.Int4 => Format.R32G32B32A32_SInt,
            CoreGfx.VertexElementFormat.UInt1 => Format.R32_UInt,
            CoreGfx.VertexElementFormat.UInt2 => Format.R32G32_UInt,
            CoreGfx.VertexElementFormat.UInt3 => Format.R32G32B32_UInt,
            CoreGfx.VertexElementFormat.UInt4 => Format.R32G32B32A32_UInt,
            CoreGfx.VertexElementFormat.Half2 => Format.R16G16_Float,
            CoreGfx.VertexElementFormat.Half4 => Format.R16G16B16A16_Float,
            CoreGfx.VertexElementFormat.Short2 => Format.R16G16_SInt,
            CoreGfx.VertexElementFormat.Short4 => Format.R16G16B16A16_SInt,
            CoreGfx.VertexElementFormat.UShort2 => Format.R16G16_UInt,
            CoreGfx.VertexElementFormat.UShort4 => Format.R16G16B16A16_UInt,
            CoreGfx.VertexElementFormat.NormShort2 => Format.R16G16_SNorm,
            CoreGfx.VertexElementFormat.NormShort4 => Format.R16G16B16A16_SNorm,
            CoreGfx.VertexElementFormat.UNormShort2 => Format.R16G16_UNorm,
            CoreGfx.VertexElementFormat.UNormShort4 => Format.R16G16B16A16_UNorm,
            CoreGfx.VertexElementFormat.Byte2 => Format.R8G8_SInt,
            CoreGfx.VertexElementFormat.Byte4 => Format.R8G8B8A8_SInt,
            CoreGfx.VertexElementFormat.UByte2 => Format.R8G8_UInt,
            CoreGfx.VertexElementFormat.UByte4 => Format.R8G8B8A8_UInt,
            CoreGfx.VertexElementFormat.NormByte2 => Format.R8G8_SNorm,
            CoreGfx.VertexElementFormat.NormByte4 => Format.R8G8B8A8_SNorm,
            CoreGfx.VertexElementFormat.UNormByte2 => Format.R8G8_UNorm,
            CoreGfx.VertexElementFormat.UNormByte4 => Format.R8G8B8A8_UNorm,
            _ => throw new NotSupportedException()
        };

    public static string ToD3D11SemanticName(this CoreGfx.VertexElementSemantic semantic) =>
        semantic switch
        {
            CoreGfx.VertexElementSemantic.Position => "POSITION",
            CoreGfx.VertexElementSemantic.Color => "COLOR",
            CoreGfx.VertexElementSemantic.TexCoord => "TEXCOORD",
            CoreGfx.VertexElementSemantic.Normal => "NORMAL",
            CoreGfx.VertexElementSemantic.Tangent => "TANGENT",
            CoreGfx.VertexElementSemantic.BlendIndices => "BLENDINDICES",
            CoreGfx.VertexElementSemantic.BlendWeight => "BLENDWEIGHT",
            _ => throw new NotSupportedException()
        };

    public static PrimitiveTopology ToD3D11PrimitiveTopology(this CoreGfx.PrimitiveTopology topology) =>
        topology switch
        {
            CoreGfx.PrimitiveTopology.Undefined => PrimitiveTopology.Undefined,
            CoreGfx.PrimitiveTopology.PointList => PrimitiveTopology.PointList,
            CoreGfx.PrimitiveTopology.LineList => PrimitiveTopology.LineList,
            CoreGfx.PrimitiveTopology.LineStrip => PrimitiveTopology.LineStrip,
            CoreGfx.PrimitiveTopology.TriangleList => PrimitiveTopology.TriangleList,
            CoreGfx.PrimitiveTopology.TriangleStrip => PrimitiveTopology.TriangleStrip,
            CoreGfx.PrimitiveTopology.TriangleFan => PrimitiveTopology.TriangleFan,
            CoreGfx.PrimitiveTopology.TriangleListWithAdjacency => PrimitiveTopology.TriangleListAdjacency,
            CoreGfx.PrimitiveTopology.TriangleStripWithAdjacency => PrimitiveTopology.TriangleStripAdjacency,
            _ => throw new NotSupportedException()
        };
}
