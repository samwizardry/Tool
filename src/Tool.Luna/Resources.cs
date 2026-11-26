using Tool.Assets;
using Tool.Graphics;

namespace Tool.Luna;

public static class Resources
{
    public static RasterStateHandle CullNone;
    public static RasterStateHandle Solid;
    public static RasterStateHandle Wireframe;

    public static BlendStateHandle Opaque;
    public static BlendStateHandle Transparent;

    public static SamplerHandle Bilinear;
    public static SamplerHandle Trilinear;
    public static SamplerHandle Anisotropic16;

    public static AssetHandle<TextureAsset> WhitePixel;

    public static void Load(GraphicsDevice device, AssetScope scope)
    {
        CullNone = device.GetOrCreateRasterState(RasterStateDescription.CullNone);
        Solid = device.GetOrCreateRasterState(RasterStateDescription.CullBack);
        Wireframe = device.GetOrCreateRasterState(RasterStateDescription.Wireframe);

        Opaque = device.GetOrCreateBlendState(BlendStateDescription.Opaque);
        Transparent = device.GetOrCreateBlendState(BlendStateDescription.NonPremultiplied);

        Bilinear = device.GetOrCreateSampler(SamplerDescription.Bilinear);
        Trilinear = device.GetOrCreateSampler(SamplerDescription.LinearWrap);
        Anisotropic16 = device.GetOrCreateSampler(SamplerDescription.AnisotropicWrap(16.0f));

        WhitePixel = scope.CreateTexture(new byte[] { 255, 255, 255, 127 }, TextureDescription.Texture2D(1u, 1u));
    }

    public static void Unload(GraphicsDevice device)
    {
    }
}
