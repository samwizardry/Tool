// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Vortice.Mathematics;

namespace Tool.Graphics;

[InlineArray(Constants.MaxBindingLayouts)]
public struct BindingSetArray : IEquatable<BindingSetArray>
{
    private BindingSetHandle _binding0;

    public BindingSetArray(params ReadOnlySpan<BindingSetHandle> bindings)
    {
        if (bindings.Length > Constants.MaxBindingLayouts)
        {
            throw new ArgumentOutOfRangeException(nameof(bindings));
        }

        for (int i = 0; i < bindings.Length; i++)
        {
            this[i] = bindings[i];
        }
    }

    public readonly bool Equals(BindingSetArray other)
    {
        for (int i = 0; i < Constants.MaxBindingLayouts; i++)
        {
            if (this[i] != other[i])
            {
                return false;
            }
        }

        return true;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj) =>
        obj is BindingSetArray other && Equals(other);

    public override readonly int GetHashCode()
    {
        HashCode hash = default;

        for (int i = 0; i < Constants.MaxBindingLayouts; i++)
        {
            hash.Add(this[i]);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(BindingSetArray left, BindingSetArray right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BindingSetArray left, BindingSetArray right)
    {
        return !(left == right);
    }
}

public readonly record struct VertexBufferBinding
{
    public readonly BufferHandle Buffer;
    public readonly uint Slot;
    public readonly uint ByteOffset;

    public VertexBufferBinding(
        BufferHandle buffer,
        uint slot = 0,
        uint byteOffset = 0)
    {
        Buffer = buffer;
        Slot = slot;
        ByteOffset = byteOffset;
    }
}

public readonly record struct IndexBufferBinding
{
    public readonly BufferHandle Buffer;
    public readonly IndexFormat Format;
    public readonly uint ByteOffset;

    public IndexBufferBinding(
        BufferHandle buffer,
        IndexFormat format,
        uint byteOffset = 0)
    {
        Buffer = buffer;
        Format = format;
        ByteOffset = byteOffset;
    }
}

public readonly record struct GraphicsState
{
    public readonly GraphicsPipelineHandle Pipeline;
    public readonly ViewportState Viewport;
    public readonly Color4 BlendConstantColor;
    public readonly BindingSetArray Bindings;
    public readonly VertexBufferBinding VertexBuffer;
    public readonly IndexBufferBinding IndexBuffer;

    public GraphicsState(
        GraphicsPipelineHandle pipeline,
        ViewportState viewport = default,
        Color4 blendConstantColor = default,
        BindingSetArray bindings = default,
        VertexBufferBinding vertexBuffer = default,
        IndexBufferBinding indexBuffer = default)
    {
        Pipeline = pipeline;
        Viewport = viewport;
        BlendConstantColor = blendConstantColor;
        Bindings = bindings;
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
    }
}
