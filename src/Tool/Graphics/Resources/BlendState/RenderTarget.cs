namespace Tool.Graphics.Resources.BlendState;

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
