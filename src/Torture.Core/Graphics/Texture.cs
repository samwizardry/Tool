// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

namespace Torture.Core.Graphics;

/// <summary>
/// Represents a GPU texture resource (1D, 2D, 3D, Cube, or Array).
/// </summary>
public abstract class Texture : IDisposable
{
    protected bool _disposed;

    public uint Width { get; protected init; }

    public uint Height { get; protected init; }

    public uint Depth { get; protected init; }

    public uint ArraySize { get; protected init; }

    public uint MipLevels { get; protected init; }

    public Format Format { get; protected init; }

    public TextureDimension Dimension { get; protected init; }

    public TextureUsage Usage { get; protected init; }

    public abstract void Dispose();
}
