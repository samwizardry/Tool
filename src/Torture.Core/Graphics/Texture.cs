// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

namespace Torture.Core.Graphics;

/// <summary>
/// Represents a GPU texture resource (1D, 2D, 3D, Cube, or Array).
/// </summary>
public abstract class Texture : IDisposable
{
    protected readonly TextureDescription _desc;
    protected bool _disposed;

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

    public abstract void Dispose();
}
