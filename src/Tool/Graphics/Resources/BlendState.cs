using Tool.Collections;

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

        /// <summary>
        /// Opaque by default.
        /// </summary>
        public RenderTarget() : this(blendEnable: false) { }

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

        public static RenderTarget Opaque => new();
    }

    public readonly RenderTarget RenderTarget0;
    public readonly RenderTarget RenderTarget1;
    public readonly RenderTarget RenderTarget2;
    public readonly RenderTarget RenderTarget3;
    public readonly RenderTarget RenderTarget4;
    public readonly RenderTarget RenderTarget5;
    public readonly RenderTarget RenderTarget6;
    public readonly RenderTarget RenderTarget7;
    public readonly bool AlphaToCoverageEnable;

    /// <summary>
    /// Opaque by default.
    /// </summary>
    public BlendStateDescription() : this(renderTarget0: RenderTarget.Opaque) { }

    public BlendStateDescription(
        RenderTarget? renderTarget0 = null,
        RenderTarget? renderTarget1 = null,
        RenderTarget? renderTarget2 = null,
        RenderTarget? renderTarget3 = null,
        RenderTarget? renderTarget4 = null,
        RenderTarget? renderTarget5 = null,
        RenderTarget? renderTarget6 = null,
        RenderTarget? renderTarget7 = null,
        bool alphaToCoverageEnable = false)
    {
        RenderTarget0 = renderTarget0 ?? RenderTarget.Opaque;
        RenderTarget1 = renderTarget1 ?? RenderTarget.Opaque;
        RenderTarget2 = renderTarget2 ?? RenderTarget.Opaque;
        RenderTarget3 = renderTarget3 ?? RenderTarget.Opaque;
        RenderTarget4 = renderTarget4 ?? RenderTarget.Opaque;
        RenderTarget5 = renderTarget5 ?? RenderTarget.Opaque;
        RenderTarget6 = renderTarget6 ?? RenderTarget.Opaque;
        RenderTarget7 = renderTarget7 ?? RenderTarget.Opaque;

        AlphaToCoverageEnable = alphaToCoverageEnable;
    }

    public BlendStateDescription(BlendFactor srcBlend, BlendFactor destBlend, BlendFactor srcBlendAlpha, BlendFactor destBlendAlpha)
    {
        RenderTarget0 = new RenderTarget(blendEnable: IsBlendEnabled(srcBlend, destBlend, srcBlendAlpha, destBlendAlpha),
            srcBlend: srcBlend,
            destBlend: destBlend,
            blendOp: BlendOp.Add,
            srcBlendAlpha: srcBlendAlpha,
            destBlendAlpha: destBlendAlpha,
            blendOpAlpha: BlendOp.Add,
            colorWriteMask: ColorMask.All);

        RenderTarget1 = RenderTarget0;
        RenderTarget2 = RenderTarget0;
        RenderTarget3 = RenderTarget0;
        RenderTarget4 = RenderTarget0;
        RenderTarget5 = RenderTarget0;
        RenderTarget6 = RenderTarget0;
        RenderTarget7 = RenderTarget0;

        AlphaToCoverageEnable = false;
    }

    public RenderTarget this[int index] => index switch
    {
        0 => RenderTarget0,
        1 => RenderTarget1,
        2 => RenderTarget2,
        3 => RenderTarget3,
        4 => RenderTarget4,
        5 => RenderTarget5,
        6 => RenderTarget6,
        7 => RenderTarget7,
        _ => throw new IndexOutOfRangeException()
    };

    public static BlendStateDescription Opaque => new();
    public static BlendStateDescription NonPremultiplied => new(BlendFactor.SrcAlpha, BlendFactor.InvSrcAlpha, BlendFactor.One, BlendFactor.InvSrcAlpha);
    public static BlendStateDescription PremultipliedAlpha => new(BlendFactor.One, BlendFactor.InvSrcAlpha, BlendFactor.One, BlendFactor.InvSrcAlpha);
    public static BlendStateDescription Additive => new(BlendFactor.SrcAlpha, BlendFactor.One, BlendFactor.One, BlendFactor.One);

    private static bool IsBlendEnabled(BlendFactor srcBlend, BlendFactor destBlend, BlendFactor srcBlendAlpha, BlendFactor destBlendAlpha) =>
        srcBlendAlpha != BlendFactor.One ||
        destBlendAlpha != BlendFactor.Zero ||
        srcBlend != BlendFactor.One ||
        destBlend != BlendFactor.Zero;

    public static BlendStateDescription Uniform(in RenderTarget target, bool alphaToCoverageEnable = false) =>
        new(target, target, target, target, target, target, target, target, alphaToCoverageEnable);
}

public readonly record struct BlendStateHandle
{
    public readonly SlotHandle Raw;

    public BlendStateHandle(SlotHandle raw)
    {
        Raw = raw;
    }

    public static BlendStateHandle Invalid => default;
    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
