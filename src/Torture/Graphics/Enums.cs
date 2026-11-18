// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

namespace Torture.Graphics;

public enum GraphicsAPI : byte
{
    Default = 0,
    D3D11,
    D3D12,
    Vulkan,
    Headless
}

[Flags]
public enum BufferUsage : ushort
{
    None = 0,
    VertexBuffer = 1 << 0,
    IndexBuffer = 1 << 1,
    ConstantBuffer = 1 << 2,
    StructuredBuffer = 1 << 3,
    IndirectArguments = 1 << 4,
    ShaderResource = 1 << 5,
    UnorderedAccess = 1 << 6,
    RawBuffer = 1 << 7
}

[Flags]
public enum TextureUsage : byte
{
    None = 0,
    ShaderResource = 1 << 0,
    RenderTarget = 1 << 1,
    DepthStencil = 1 << 2,
    UnorderedAccess = 1 << 3
}

public enum CpuAccess : byte
{
    /// <summary>
    /// Resource is accessible only by the GPU (best performance).
    /// </summary>
    None = 0,

    /// <summary>
    /// CPU can write to the resource (Dynamic / Upload).
    /// </summary>
    Write,

    /// <summary>
    /// CPU can read from the resource (Staging / Readback).
    /// </summary>
    Read
}

/// <summary>
/// Resource data formats modeled after nvrhi::Format.
/// </summary>
public enum Format : byte
{
    Unknown = 0,

    R8_UInt,
    R8_SInt,
    R8_UNorm,
    R8_SNorm,
    RG8_UInt,
    RG8_SInt,
    RG8_UNorm,
    RG8_SNorm,
    R16_UInt,
    R16_SInt,
    R16_UNorm,
    R16_SNorm,
    R16_Float,
    BGRA4_UNorm,
    B5G6R5_UNorm,
    B5G5R5A1_UNorm,
    RGBA8_UInt,
    RGBA8_SInt,
    RGBA8_UNorm,
    RGBA8_SNorm,
    BGRA8_UNorm,
    SRGBA8_UNorm,
    SBGRA8_UNorm,
    R10G10B10A2_UNorm,
    R11G11B10_Float,
    RG16_UInt,
    RG16_SInt,
    RG16_UNorm,
    RG16_SNorm,
    RG16_Float,
    R32_UInt,
    R32_SInt,
    R32_Float,
    RGBA16_UInt,
    RGBA16_SInt,
    RGBA16_Float,
    RGBA16_UNorm,
    RGBA16_SNorm,
    RG32_UInt,
    RG32_SInt,
    RG32_Float,
    RGB32_UInt,
    RGB32_SInt,
    RGB32_Float,
    RGBA32_UInt,
    RGBA32_SInt,
    RGBA32_Float,

    D16,
    D24S8,
    X24G8_UInt,
    D32,
    D32S8,
    X32G8_UInt,

    BC1_UNorm,
    BC1_UNorm_Srgb,
    BC2_UNorm,
    BC2_UNorm_Srgb,
    BC3_UNorm,
    BC3_UNorm_Srgb,
    BC4_UNorm,
    BC4_SNorm,
    BC5_UNorm,
    BC5_SNorm,
    BC6H_UFloat,
    BC6H_SFloat,
    BC7_UNorm,
    BC7_UNorm_Srgb,

    Count
}

public enum TextureDimension : byte
{
    Unknown = 0,
    Texture1D,
    Texture1DArray,
    Texture2D,
    Texture2DArray,
    TextureCube,
    TextureCubeArray,
    Texture2DMS,
    Texture2DMSArray,
    Texture3D
}

/// <summary>
/// The value of an element is equal to the byte stride of the index buffer.
/// </summary>
public enum IndexFormat : byte
{
    UInt16 = 2,
    UInt32 = 4
}

[Flags]
public enum ShaderType : ushort
{
    None = 0x0000,
    Vertex = 0x0001,
    Hull = 0x0002,
    Domain = 0x0004,
    Geometry = 0x0008,
    Pixel = 0x0010,
    Compute = 0x0020,
    Amplification = 0x0040,
    Mesh = 0x0080,
    AllGraphics = 0x00DF,
    RayGeneration = 0x0100,
    AnyHit = 0x0200,
    ClosestHit = 0x0400,
    Miss = 0x0800,
    Intersection = 0x1000,
    Callable = 0x2000,
    AllRayTracing = 0x3F00,
    All = 0x3FFF
}

public enum PrimitiveTopology : byte
{
    Undefined = 0,
    PointList,
    LineList,
    LineStrip,
    TriangleList,
    TriangleStrip,
    TriangleFan,
    TriangleListWithAdjacency,
    TriangleStripWithAdjacency,
    PatchList
}

public enum BlendFactor : byte
{
    Zero = 1,
    One = 2,
    SrcColor = 3,
    InvSrcColor = 4,
    SrcAlpha = 5,
    InvSrcAlpha = 6,
    DstAlpha = 7,
    InvDstAlpha = 8,
    DstColor = 9,
    InvDstColor = 10,
    SrcAlphaSaturate = 11,
    ConstantColor = 14,
    InvConstantColor = 15,
    Src1Color = 16,
    InvSrc1Color = 17,
    Src1Alpha = 18,
    InvSrc1Alpha = 19
}

public enum BlendOp : byte
{
    Add = 1,
    Subtract = 2,
    ReverseSubtract = 3,
    Min = 4,
    Max = 5
}

[Flags]
public enum ColorMask : byte
{
    None = 0,
    Red = 1 << 0,
    Green = 1 << 1,
    Blue = 1 << 2,
    Alpha = 1 << 3,
    All = Red | Green | Blue | Alpha
}

public enum StencilOp : byte
{
    Keep = 1,
    Zero = 2,
    Replace = 3,
    IncrementAndClamp = 4,
    DecrementAndClamp = 5,
    Invert = 6,
    IncrementAndWrap = 7,
    DecrementAndWrap = 8
}

public enum ComparisonFunc : byte
{
    Never = 1,
    Less = 2,
    Equal = 3,
    LessOrEqual = 4,
    Greater = 5,
    NotEqual = 6,
    GreaterOrEqual = 7,
    Always = 8
}

public enum RasterFillMode : byte
{
    Solid = 0,
    Wireframe
}

public enum RasterCullMode : byte
{
    Back = 0,
    Front,
    None
}

public enum SamplerAddressMode : byte
{
    Clamp = 0,
    Wrap,
    Border,
    Mirror,
    MirrorOnce
}

public enum SamplerReductionType : byte
{
    Standard = 0,
    Comparison,
    Minimum,
    Maximum
}
