using Tool.Assets;
using Tool.Graphics;

using Vortice.Mathematics;

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

    public static ViewportState Viewport;

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

        Viewport = new ViewportState(new Viewport(device.ClientSize.Width, device.ClientSize.Height));
    }

    public static void Unload(GraphicsDevice device)
    {
    }

    public static void OnResize(int width, int height)
    {
        Viewport = new ViewportState(new Viewport(width, height));
    }
}
