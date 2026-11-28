using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Tool.Graphics;

public readonly record struct BlendStateDescription
{
    public readonly record struct RenderTarget
    {
        public readonly bool BlendEnable;
        public readonly BlendFactor SrcBlend;
        public readonly BlendFactor DestBlend;
        public readonly BlendOp BlendOp;
        public readonly BlendFactor SrcBlendAlpha;
        public readonly BlendFactor DestBlendAlpha;
        public readonly BlendOp BlendOpAlpha;
        public readonly ColorMask ColorWriteMask;

        public RenderTarget(
            bool blendEnable = false,
            BlendFactor srcBlend = BlendFactor.One,
            BlendFactor destBlend = BlendFactor.Zero,
            BlendOp blendOp = BlendOp.Add,
            BlendFactor srcBlendAlpha = BlendFactor.One,
            BlendFactor destBlendAlpha = BlendFactor.Zero,
            BlendOp blendOpAlpha = BlendOp.Add,
            ColorMask colorWriteMask = ColorMask.All)
        {
            BlendEnable = blendEnable;
            SrcBlend = srcBlend;
            DestBlend = destBlend;
            BlendOp = blendOp;
            SrcBlendAlpha = srcBlendAlpha;
            DestBlendAlpha = destBlendAlpha;
            BlendOpAlpha = blendOpAlpha;
            ColorWriteMask = colorWriteMask;
        }

        /// <summary>
        /// Opaque by default.
        /// </summary>
        public RenderTarget() : this(blendEnable: false) { }

        public static RenderTarget Opaque => new RenderTarget();
    }

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

    public readonly RenderTargetArray RenderTargets;
    public readonly bool AlphaToCoverageEnable;

    /// <summary>
    /// Opaque by default.
    /// </summary>
    public BlendStateDescription()
    {
        RenderTargets = RenderTargetArray.CreateUniform(RenderTarget.Opaque);
        AlphaToCoverageEnable = false;
    }

    public BlendStateDescription(RenderTargetArray renderTargets, bool alphaToCoverageEnable = false)
    {
        RenderTargets = renderTargets;
        AlphaToCoverageEnable = alphaToCoverageEnable;
    }

    public BlendStateDescription(BlendFactor srcBlend, BlendFactor destBlend, BlendFactor srcBlendAlpha, BlendFactor destBlendAlpha)
    {
        var rt = new RenderTarget(blendEnable: IsBlendEnabled(srcBlend, destBlend, srcBlendAlpha, destBlendAlpha),
            srcBlend: srcBlend,
            destBlend: destBlend,
            blendOp: BlendOp.Add,
            srcBlendAlpha: srcBlendAlpha,
            destBlendAlpha: destBlendAlpha,
            blendOpAlpha: BlendOp.Add,
            colorWriteMask: ColorMask.All);

        RenderTargets = RenderTargetArray.CreateUniform(rt);
        AlphaToCoverageEnable = false;
    }

    public static BlendStateDescription Opaque => new BlendStateDescription();
    public static BlendStateDescription NonPremultiplied => new BlendStateDescription(BlendFactor.SrcAlpha, BlendFactor.InvSrcAlpha, BlendFactor.One, BlendFactor.InvSrcAlpha);
    public static BlendStateDescription PremultipliedAlpha => new BlendStateDescription(BlendFactor.One, BlendFactor.InvSrcAlpha, BlendFactor.One, BlendFactor.InvSrcAlpha);
    public static BlendStateDescription Additive => new BlendStateDescription(BlendFactor.SrcAlpha, BlendFactor.One, BlendFactor.One, BlendFactor.One);

    public static BlendStateDescription Uniform(RenderTarget renderTarget, bool alphaToCoverageEnable = false) =>
        new BlendStateDescription(RenderTargetArray.CreateUniform(renderTarget), alphaToCoverageEnable);

    private static bool IsBlendEnabled(BlendFactor srcBlend, BlendFactor destBlend, BlendFactor srcBlendAlpha, BlendFactor destBlendAlpha) =>
        srcBlendAlpha != BlendFactor.One ||
        destBlendAlpha != BlendFactor.Zero ||
        srcBlend != BlendFactor.One ||
        destBlend != BlendFactor.Zero;
}


