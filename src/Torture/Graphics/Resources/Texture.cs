// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using System.Numerics;

namespace Torture.Graphics;

/// <summary>
/// Describes a texture resource to be created on the graphics device.
/// </summary>
public readonly record struct TextureDescription
{
    public TextureDescription(
        uint width,
        uint height = 1,
        uint depth = 1,
        uint arraySize = 1,
        uint mipLevels = 1,
        uint sampleCount = 1,
        uint sampleQuality = 0,
        Format format = Format.RGBA8_UNorm,
        TextureDimension dimension = TextureDimension.Texture2D,
        TextureUsage usage = TextureUsage.ShaderResource,
        CpuAccess cpuAccess = CpuAccess.None)
    {
        Width = width;
        Height = height;
        Depth = depth;
        ArraySize = arraySize;
        MipLevels = mipLevels == 0 ? CalculateMipLevels(width, height, depth) : mipLevels;
        SampleCount = sampleCount;
        SampleQuality = sampleQuality;
        Format = format;
        Dimension = dimension;
        Usage = usage;
        CpuAccess = cpuAccess;
    }

    public readonly uint Width { get; init; }
    public readonly uint Height { get; init; }
    public readonly uint Depth { get; init; }
    public readonly uint ArraySize { get; init; }
    public readonly uint MipLevels { get; init; }
    public readonly uint SampleCount { get; init; }
    public readonly uint SampleQuality { get; init; }
    public readonly Format Format { get; init; }
    public readonly TextureDimension Dimension { get; init; }
    public readonly TextureUsage Usage { get; init; }
    public readonly CpuAccess CpuAccess { get; init; }

    public static TextureDescription Texture2D(
        uint width,
        uint height,
        Format format = Format.RGBA8_UNorm,
        uint mipLevels = 1,
        TextureUsage usage = TextureUsage.ShaderResource,
        CpuAccess cpuAccess = CpuAccess.None) =>
        new(
            width: width,
            height: height,
            depth: 1,
            arraySize: 1,
            mipLevels: mipLevels,
            sampleCount: 1,
            sampleQuality: 0,
            format: format,
            dimension: TextureDimension.Texture2D,
            usage: usage,
            cpuAccess: cpuAccess);

    public static TextureDescription TextureCube(
        uint size,
        Format format = Format.RGBA8_UNorm,
        uint mipLevels = 1,
        TextureUsage usage = TextureUsage.ShaderResource) =>
        new(
            width: size,
            height: size,
            depth: 1,
            arraySize: 6,
            mipLevels: mipLevels,
            sampleCount: 1,
            sampleQuality: 0,
            format: format,
            dimension: TextureDimension.TextureCube,
            usage: usage,
            cpuAccess: CpuAccess.None);

    public static TextureDescription Texture3D(
        uint width,
        uint height,
        uint depth,
        Format format = Format.RGBA8_UNorm,
        uint mipLevels = 1,
        TextureUsage usage = TextureUsage.ShaderResource) =>
        new(
            width: width,
            height: height,
            depth: depth,
            arraySize: 1,
            mipLevels: mipLevels,
            sampleCount: 1,
            sampleQuality: 0,
            format: format,
            dimension: TextureDimension.Texture3D,
            usage: usage,
            cpuAccess: CpuAccess.None);

    /// <summary>
    /// Calculates the full mipmap chain count down to 1x1x1 for the given dimensions.
    /// </summary>
    public static uint CalculateMipLevels(uint width, uint height, uint depth = 1) =>
        (uint)BitOperations.Log2(System.Math.Max(System.Math.Max(width, height), depth)) + 1u;
}

/// <summary>
/// Represents a GPU texture resource (1D, 2D, 3D, Cube, or Array).
/// </summary>
public abstract class Texture : GraphicsResource, IBindableResource
{
    protected readonly TextureDescription _desc;

    protected Texture(in TextureDescription desc)
    {
        _desc = desc;
    }

    public ref readonly TextureDescription Desc => ref _desc;

    public uint Width => _desc.Width;

    public uint Height => _desc.Height;

    public uint Depth => _desc.Depth;

    public uint ArraySize => _desc.ArraySize;

    public uint MipLevels => _desc.MipLevels;

    public Format Format => _desc.Format;

    public TextureDimension Dimension => _desc.Dimension;

    public TextureUsage Usage => _desc.Usage;
}
