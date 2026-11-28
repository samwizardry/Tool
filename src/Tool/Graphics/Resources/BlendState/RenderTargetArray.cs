using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Tool.Graphics.Resources.BlendState;

[InlineArray(Constants.MaxRenderTargets)]
public struct RenderTargetArray : IEquatable<RenderTargetArray>
{
    private RenderTarget _element0;

    public RenderTargetArray()
    {
        for (int i = 0; i < Constants.MaxRenderTargets; i++)
        {
            this[i] = RenderTarget.Opaque;
        }
    }

    public RenderTargetArray(params ReadOnlySpan<RenderTarget> renderTargets)
    {
        if (renderTargets.Length > Constants.MaxRenderTargets)
        {
            throw new ArgumentOutOfRangeException(nameof(renderTargets));
        }

        int i = 0;
        for (; i < renderTargets.Length; i++)
        {
            this[i] = renderTargets[i];
        }

        for (; i < Constants.MaxRenderTargets; i++)
        {
            this[i] = RenderTarget.Opaque;
        }
    }

    public static RenderTargetArray CreateUniform(RenderTarget renderTarget)
    {
        RenderTargetArray array = default;

        for (int i = 0; i < Constants.MaxRenderTargets; i++)
        {
            array[i] = renderTarget;
        }

        return array;
    }

    public readonly bool Equals(RenderTargetArray other)
    {
        for (int i = 0; i < Constants.MaxRenderTargets; i++)
        {
            if (this[i] != other[i])
                return false;
        }

        return true;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj) =>
        obj is RenderTargetArray other && Equals(other);

    public override readonly int GetHashCode()
    {
        HashCode hash = new HashCode();
        for (int i = 0; i < Constants.MaxRenderTargets; i++)
        {
            hash.Add(this[i]);
        }
        return hash.ToHashCode();
    }

    public static bool operator ==(RenderTargetArray left, RenderTargetArray right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RenderTargetArray left, RenderTargetArray right)
    {
        return !(left == right);
    }
}
