// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Vortice.Direct3D;
using Vortice.DXGI;

namespace Torture.Direct3D11;

internal static class Extensions
{
    public readonly record struct DxgiFormatMapping(
        Graphics.Format AbstractFormat,
        Format ResourceFormat,
        Format SrvFormat,
        Format RtvDsvFormat);

    private static readonly DxgiFormatMapping[] FormatMappings =
    [
        new(Graphics.Format.Unknown,             Format.Unknown,                 Format.Unknown,                     Format.Unknown),
        new(Graphics.Format.R8_UInt,             Format.R8_Typeless,             Format.R8_UInt,                     Format.R8_UInt),
        new(Graphics.Format.R8_SInt,             Format.R8_Typeless,             Format.R8_SInt,                     Format.R8_SInt),
        new(Graphics.Format.R8_UNorm,            Format.R8_Typeless,             Format.R8_UNorm,                    Format.R8_UNorm),
        new(Graphics.Format.R8_SNorm,            Format.R8_Typeless,             Format.R8_SNorm,                    Format.R8_SNorm),
        new(Graphics.Format.RG8_UInt,            Format.R8G8_Typeless,           Format.R8G8_UInt,                   Format.R8G8_UInt),
        new(Graphics.Format.RG8_SInt,            Format.R8G8_Typeless,           Format.R8G8_SInt,                   Format.R8G8_SInt),
        new(Graphics.Format.RG8_UNorm,           Format.R8G8_Typeless,           Format.R8G8_UNorm,                  Format.R8G8_UNorm),
        new(Graphics.Format.RG8_SNorm,           Format.R8G8_Typeless,           Format.R8G8_SNorm,                  Format.R8G8_SNorm),
        new(Graphics.Format.R16_UInt,            Format.R16_Typeless,            Format.R16_UInt,                    Format.R16_UInt),
        new(Graphics.Format.R16_SInt,            Format.R16_Typeless,            Format.R16_SInt,                    Format.R16_SInt),
        new(Graphics.Format.R16_UNorm,           Format.R16_Typeless,            Format.R16_UNorm,                   Format.R16_UNorm),
        new(Graphics.Format.R16_SNorm,           Format.R16_Typeless,            Format.R16_SNorm,                   Format.R16_SNorm),
        new(Graphics.Format.R16_Float,           Format.R16_Typeless,            Format.R16_Float,                   Format.R16_Float),
        new(Graphics.Format.BGRA4_UNorm,         Format.B4G4R4A4_UNorm,          Format.B4G4R4A4_UNorm,              Format.B4G4R4A4_UNorm),
        new(Graphics.Format.B5G6R5_UNorm,        Format.B5G6R5_UNorm,            Format.B5G6R5_UNorm,                Format.B5G6R5_UNorm),
        new(Graphics.Format.B5G5R5A1_UNorm,      Format.B5G5R5A1_UNorm,          Format.B5G5R5A1_UNorm,              Format.B5G5R5A1_UNorm),
        new(Graphics.Format.RGBA8_UInt,          Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_UInt,               Format.R8G8B8A8_UInt),
        new(Graphics.Format.RGBA8_SInt,          Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_SInt,               Format.R8G8B8A8_SInt),
        new(Graphics.Format.RGBA8_UNorm,         Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_UNorm,              Format.R8G8B8A8_UNorm),
        new(Graphics.Format.RGBA8_SNorm,         Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_SNorm,              Format.R8G8B8A8_SNorm),
        new(Graphics.Format.BGRA8_UNorm,         Format.B8G8R8A8_Typeless,       Format.B8G8R8A8_UNorm,              Format.B8G8R8A8_UNorm),
        new(Graphics.Format.SRGBA8_UNorm,        Format.R8G8B8A8_Typeless,       Format.R8G8B8A8_UNorm_SRgb,         Format.R8G8B8A8_UNorm_SRgb),
        new(Graphics.Format.SBGRA8_UNorm,        Format.B8G8R8A8_Typeless,       Format.B8G8R8A8_UNorm_SRgb,         Format.B8G8R8A8_UNorm_SRgb),
        new(Graphics.Format.R10G10B10A2_UNorm,   Format.R10G10B10A2_Typeless,    Format.R10G10B10A2_UNorm,           Format.R10G10B10A2_UNorm),
        new(Graphics.Format.R11G11B10_Float,     Format.R11G11B10_Float,         Format.R11G11B10_Float,             Format.R11G11B10_Float),
        new(Graphics.Format.RG16_UInt,           Format.R16G16_Typeless,         Format.R16G16_UInt,                 Format.R16G16_UInt),
        new(Graphics.Format.RG16_SInt,           Format.R16G16_Typeless,         Format.R16G16_SInt,                 Format.R16G16_SInt),
        new(Graphics.Format.RG16_UNorm,          Format.R16G16_Typeless,         Format.R16G16_UNorm,                Format.R16G16_UNorm),
        new(Graphics.Format.RG16_SNorm,          Format.R16G16_Typeless,         Format.R16G16_SNorm,                Format.R16G16_SNorm),
        new(Graphics.Format.RG16_Float,          Format.R16G16_Typeless,         Format.R16G16_Float,                Format.R16G16_Float),
        new(Graphics.Format.R32_UInt,            Format.R32_Typeless,            Format.R32_UInt,                    Format.R32_UInt),
        new(Graphics.Format.R32_SInt,            Format.R32_Typeless,            Format.R32_SInt,                    Format.R32_SInt),
        new(Graphics.Format.R32_Float,           Format.R32_Typeless,            Format.R32_Float,                   Format.R32_Float),
        new(Graphics.Format.RGBA16_UInt,         Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_UInt,           Format.R16G16B16A16_UInt),
        new(Graphics.Format.RGBA16_SInt,         Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_SInt,           Format.R16G16B16A16_SInt),
        new(Graphics.Format.RGBA16_Float,        Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_Float,          Format.R16G16B16A16_Float),
        new(Graphics.Format.RGBA16_UNorm,        Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_UNorm,          Format.R16G16B16A16_UNorm),
        new(Graphics.Format.RGBA16_SNorm,        Format.R16G16B16A16_Typeless,   Format.R16G16B16A16_SNorm,          Format.R16G16B16A16_SNorm),
        new(Graphics.Format.RG32_UInt,           Format.R32G32_Typeless,         Format.R32G32_UInt,                 Format.R32G32_UInt),
        new(Graphics.Format.RG32_SInt,           Format.R32G32_Typeless,         Format.R32G32_SInt,                 Format.R32G32_SInt),
        new(Graphics.Format.RG32_Float,          Format.R32G32_Typeless,         Format.R32G32_Float,                Format.R32G32_Float),
        new(Graphics.Format.RGB32_UInt,          Format.R32G32B32_Typeless,      Format.R32G32B32_UInt,              Format.R32G32B32_UInt),
        new(Graphics.Format.RGB32_SInt,          Format.R32G32B32_Typeless,      Format.R32G32B32_SInt,              Format.R32G32B32_SInt),
        new(Graphics.Format.RGB32_Float,         Format.R32G32B32_Typeless,      Format.R32G32B32_Float,             Format.R32G32B32_Float),
        new(Graphics.Format.RGBA32_UInt,         Format.R32G32B32A32_Typeless,   Format.R32G32B32A32_UInt,           Format.R32G32B32A32_UInt),
        new(Graphics.Format.RGBA32_SInt,         Format.R32G32B32A32_Typeless,   Format.R32G32B32A32_SInt,           Format.R32G32B32A32_SInt),
        new(Graphics.Format.RGBA32_Float,        Format.R32G32B32A32_Typeless,   Format.R32G32B32A32_Float,          Format.R32G32B32A32_Float),
        new(Graphics.Format.D16,                 Format.R16_Typeless,            Format.R16_UNorm,                   Format.D16_UNorm),
        new(Graphics.Format.D24S8,               Format.R24G8_Typeless,          Format.R24_UNorm_X8_Typeless,       Format.D24_UNorm_S8_UInt),
        new(Graphics.Format.X24G8_UInt,          Format.R24G8_Typeless,          Format.X24_Typeless_G8_UInt,        Format.D24_UNorm_S8_UInt),
        new(Graphics.Format.D32,                 Format.R32_Typeless,            Format.R32_Float,                   Format.D32_Float),
        new(Graphics.Format.D32S8,               Format.R32G8X24_Typeless,       Format.R32_Float_X8X24_Typeless,    Format.D32_Float_S8X24_UInt),
        new(Graphics.Format.X32G8_UInt,          Format.R32G8X24_Typeless,       Format.X32_Typeless_G8X24_UInt,     Format.D32_Float_S8X24_UInt),
        new(Graphics.Format.BC1_UNorm,           Format.BC1_Typeless,            Format.BC1_UNorm,                   Format.BC1_UNorm),
        new(Graphics.Format.BC1_UNorm_Srgb,      Format.BC1_Typeless,            Format.BC1_UNorm_SRgb,              Format.BC1_UNorm_SRgb),
        new(Graphics.Format.BC2_UNorm,           Format.BC2_Typeless,            Format.BC2_UNorm,                   Format.BC2_UNorm),
        new(Graphics.Format.BC2_UNorm_Srgb,      Format.BC2_Typeless,            Format.BC2_UNorm_SRgb,              Format.BC2_UNorm_SRgb),
        new(Graphics.Format.BC3_UNorm,           Format.BC3_Typeless,            Format.BC3_UNorm,                   Format.BC3_UNorm),
        new(Graphics.Format.BC3_UNorm_Srgb,      Format.BC3_Typeless,            Format.BC3_UNorm_SRgb,              Format.BC3_UNorm_SRgb),
        new(Graphics.Format.BC4_UNorm,           Format.BC4_Typeless,            Format.BC4_UNorm,                   Format.BC4_UNorm),
        new(Graphics.Format.BC4_SNorm,           Format.BC4_Typeless,            Format.BC4_SNorm,                   Format.BC4_SNorm),
        new(Graphics.Format.BC5_UNorm,           Format.BC5_Typeless,            Format.BC5_UNorm,                   Format.BC5_UNorm),
        new(Graphics.Format.BC5_SNorm,           Format.BC5_Typeless,            Format.BC5_SNorm,                   Format.BC5_SNorm),
        new(Graphics.Format.BC6H_UFloat,         Format.BC6H_Typeless,           Format.BC6H_Uf16,                   Format.BC6H_Uf16),
        new(Graphics.Format.BC6H_SFloat,         Format.BC6H_Typeless,           Format.BC6H_Sf16,                   Format.BC6H_Sf16),
        new(Graphics.Format.BC7_UNorm,           Format.BC7_Typeless,            Format.BC7_UNorm,                   Format.BC7_UNorm),
        new(Graphics.Format.BC7_UNorm_Srgb,      Format.BC7_Typeless,            Format.BC7_UNorm_SRgb,              Format.BC7_UNorm_SRgb)
    ];

    public static ref readonly DxgiFormatMapping GetDxgiFormatMapping(this Graphics.Format format) =>
        ref FormatMappings[(int)format];

    public static Format ToDXGIFormat(this Graphics.Format format) =>
        FormatMappings[(int)format].SrvFormat;

    public static Format ToDXGIFormat(this Graphics.IndexFormat format) =>
        format switch
        {
            Graphics.IndexFormat.UInt16 => Format.R16_UInt,
            Graphics.IndexFormat.UInt32 => Format.R32_UInt,
            _ => throw new NotSupportedException()
        };

    public static Format ToDXGIFormat(this Graphics.VertexElementFormat format) =>
        format switch
        {
            Graphics.VertexElementFormat.Float1 => Format.R32_Float,
            Graphics.VertexElementFormat.Float2 => Format.R32G32_Float,
            Graphics.VertexElementFormat.Float3 => Format.R32G32B32_Float,
            Graphics.VertexElementFormat.Float4 => Format.R32G32B32A32_Float,
            Graphics.VertexElementFormat.Int1 => Format.R32_SInt,
            Graphics.VertexElementFormat.Int2 => Format.R32G32_SInt,
            Graphics.VertexElementFormat.Int3 => Format.R32G32B32_SInt,
            Graphics.VertexElementFormat.Int4 => Format.R32G32B32A32_SInt,
            Graphics.VertexElementFormat.UInt1 => Format.R32_UInt,
            Graphics.VertexElementFormat.UInt2 => Format.R32G32_UInt,
            Graphics.VertexElementFormat.UInt3 => Format.R32G32B32_UInt,
            Graphics.VertexElementFormat.UInt4 => Format.R32G32B32A32_UInt,
            Graphics.VertexElementFormat.Half2 => Format.R16G16_Float,
            Graphics.VertexElementFormat.Half4 => Format.R16G16B16A16_Float,
            Graphics.VertexElementFormat.Short2 => Format.R16G16_SInt,
            Graphics.VertexElementFormat.Short4 => Format.R16G16B16A16_SInt,
            Graphics.VertexElementFormat.UShort2 => Format.R16G16_UInt,
            Graphics.VertexElementFormat.UShort4 => Format.R16G16B16A16_UInt,
            Graphics.VertexElementFormat.NormShort2 => Format.R16G16_SNorm,
            Graphics.VertexElementFormat.NormShort4 => Format.R16G16B16A16_SNorm,
            Graphics.VertexElementFormat.UNormShort2 => Format.R16G16_UNorm,
            Graphics.VertexElementFormat.UNormShort4 => Format.R16G16B16A16_UNorm,
            Graphics.VertexElementFormat.Byte2 => Format.R8G8_SInt,
            Graphics.VertexElementFormat.Byte4 => Format.R8G8B8A8_SInt,
            Graphics.VertexElementFormat.UByte2 => Format.R8G8_UInt,
            Graphics.VertexElementFormat.UByte4 => Format.R8G8B8A8_UInt,
            Graphics.VertexElementFormat.NormByte2 => Format.R8G8_SNorm,
            Graphics.VertexElementFormat.NormByte4 => Format.R8G8B8A8_SNorm,
            Graphics.VertexElementFormat.UNormByte2 => Format.R8G8_UNorm,
            Graphics.VertexElementFormat.UNormByte4 => Format.R8G8B8A8_UNorm,
            _ => throw new NotSupportedException()
        };

    public static string ToD3D11SemanticName(this Graphics.VertexElementSemantic semantic) =>
        semantic switch
        {
            Graphics.VertexElementSemantic.Position => "POSITION",
            Graphics.VertexElementSemantic.Color => "COLOR",
            Graphics.VertexElementSemantic.TexCoord => "TEXCOORD",
            Graphics.VertexElementSemantic.Normal => "NORMAL",
            Graphics.VertexElementSemantic.Tangent => "TANGENT",
            Graphics.VertexElementSemantic.BlendIndices => "BLENDINDICES",
            Graphics.VertexElementSemantic.BlendWeight => "BLENDWEIGHT",
            _ => throw new NotSupportedException()
        };

    public static PrimitiveTopology ToD3D11PrimitiveTopology(this Graphics.PrimitiveTopology topology) =>
        topology switch
        {
            Graphics.PrimitiveTopology.Undefined => PrimitiveTopology.Undefined,
            Graphics.PrimitiveTopology.PointList => PrimitiveTopology.PointList,
            Graphics.PrimitiveTopology.LineList => PrimitiveTopology.LineList,
            Graphics.PrimitiveTopology.LineStrip => PrimitiveTopology.LineStrip,
            Graphics.PrimitiveTopology.TriangleList => PrimitiveTopology.TriangleList,
            Graphics.PrimitiveTopology.TriangleStrip => PrimitiveTopology.TriangleStrip,
            Graphics.PrimitiveTopology.TriangleFan => PrimitiveTopology.TriangleFan,
            Graphics.PrimitiveTopology.TriangleListWithAdjacency => PrimitiveTopology.TriangleListAdjacency,
            Graphics.PrimitiveTopology.TriangleStripWithAdjacency => PrimitiveTopology.TriangleStripAdjacency,
            _ => throw new NotSupportedException()
        };
}
