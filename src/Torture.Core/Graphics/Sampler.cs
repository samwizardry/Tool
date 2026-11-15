// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

namespace Torture.Core.Graphics;

/// <summary>
/// Represents a GPU texture sampler state.
/// </summary>
public abstract class Sampler : IDisposable
{
    protected readonly SamplerDescription _desc;
    protected bool _disposed;

    protected Sampler(in SamplerDescription desc)
    {
        _desc = desc;
    }

    public ref readonly SamplerDescription Desc => ref _desc;

    public abstract void Dispose();
}
