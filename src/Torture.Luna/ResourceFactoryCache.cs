using StbImageSharp;

using Torture.Graphics;

namespace Torture.Luna;

public static class ResourceFactoryCache
{
    public static ShaderHandle PhongVs;
    public static ShaderHandle PhongPs;

    public static InputLayoutHandle PhongInputLayout;

    public static RasterStateHandle Solid;
    public static RasterStateHandle Wireframe;
    public static SamplerHandle Bilinear;
    public static SamplerHandle Anisotropic16;

    public static BufferHandle PerFrameCb;

    public static TextureHandle WhitePixel;
    public static TextureHandle Container;
    public static TextureHandle ContainerSpecular;

    // Pipelines
    public static GraphicsPipelineHandle PhongPipeline;

    // Binding layouts
    public static BindingLayoutHandle PhongBindingLayout;
    public static BindingLayoutHandle PhongPerFrameBindingLayout;
    public static BindingLayoutHandle PhongPerObjBindingLayout;

    // Material binding sets
    public static BindingSetHandle BoxMatBindingSet;
    public static BindingSetHandle CarMatBindingSet;

    // Per frame binding set
    public static BindingSetHandle PhongPerFrameBindingSet;

    // Materials
    public static Material BoxMaterial;
    public static Material CarMaterial;


    public static void Load(GraphicsDevice device)
    {
        // Shaders
        byte[] phongSource = File.ReadAllBytes(Path.Combine("Shaders", "Phong.hlsl"));
        PhongVs = device.CreateShader(phongSource, ShaderDescription.Vertex(debug: true, debugName: "phong.vs"));
        PhongPs = device.CreateShader(phongSource, ShaderDescription.Pixel(debug: true, debugName: "phong.ps"));

        PhongInputLayout = device.GetOrCreateInputLayout(VertexPositionNormalTexture.Layout, PhongVs);

        Solid = device.GetOrCreateRasterState(RasterStateDescription.CullBack);
        Wireframe = device.GetOrCreateRasterState(RasterStateDescription.Wireframe);
        Bilinear = device.GetOrCreateSampler(SamplerDescription.Bilinear);
        Anisotropic16 = device.GetOrCreateSampler(SamplerDescription.AnisotropicWrap(16.0f));

        PerFrameCb = device.CreateBuffer(BufferDescription.Constant<PhongPerFrame>());

        WhitePixel = device.CreateTexture(new byte[] { 255, 255, 255, 255 }, TextureDescription.Texture2D(1u, 1u));

        using (var stream = File.OpenRead("resources/textures/container2.png"))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            Container = device.CreateTexture(
                image.Data.AsSpan(),
                TextureDescription.Texture2D((uint)image.Width, (uint)image.Height, mipLevels: 0));
        }

        using (var stream = File.OpenRead("resources/textures/container2_specular.png"))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            ContainerSpecular = device.CreateTexture(
                image.Data.AsSpan(),
                TextureDescription.Texture2D((uint)image.Width, (uint)image.Height, mipLevels: 0));
        }

        // Pipelines
        PhongPipeline = device.GetOrCreateGraphicsPipeline(new GraphicsPipelineDescription(
            PhongVs,
            PhongPs,
            PhongInputLayout,
            Solid,
            PrimitiveTopology.TriangleList));

        // Binding layouts
        PhongBindingLayout = device.GetOrCreateBindingLayout(new BindingLayoutDescription(
            Visibility: ShaderType.Vertex | ShaderType.Pixel,
            Bindings: [
                new(0, BindingResourceType.TextureSrv),
                new(1, BindingResourceType.TextureSrv),
                new(0, BindingResourceType.Sampler),
                new(1, BindingResourceType.Sampler)],
            RegisterSpace: 0));

        PhongPerFrameBindingLayout = device.GetOrCreateBindingLayout(new BindingLayoutDescription(
            Visibility: ShaderType.Vertex | ShaderType.Pixel,
            Bindings: [
                new(0, BindingResourceType.ConstantBuffer),
            ],
            RegisterSpace: 0));

        PhongPerObjBindingLayout = device.GetOrCreateBindingLayout(new BindingLayoutDescription(
            Visibility: ShaderType.Vertex | ShaderType.Pixel,
            Bindings: [
                new(1, BindingResourceType.ConstantBuffer)
            ],
            RegisterSpace: 0));

        // Material binding sets
        BoxMatBindingSet = device.CreateBindingSet(
            desc: new BindingSetDescription(
                Bindings: [
                    BindingSetItem.TextureSrv(0, Container),
                    BindingSetItem.TextureSrv(1, ContainerSpecular),
                    BindingSetItem.Sampler(0, Anisotropic16)
                ]),
            layout: PhongBindingLayout);

        CarMatBindingSet = device.CreateBindingSet(
            desc: new BindingSetDescription(
                Bindings: [
                    BindingSetItem.TextureSrv(0, WhitePixel),
                    BindingSetItem.TextureSrv(1, WhitePixel),
                    BindingSetItem.Sampler(0, Bilinear)
                ]),
            layout: PhongBindingLayout);

        // Per frame binding set
        PhongPerFrameBindingSet = device.CreateBindingSet(
            desc: new BindingSetDescription(
                Bindings: [
                    BindingSetItem.ConstantBuffer(0, PerFrameCb)
                ]),
            layout: PhongPerFrameBindingLayout);

        // Materials
        BoxMaterial = new Material(PhongPipeline, BoxMatBindingSet);
        CarMaterial = new Material(PhongPipeline, CarMatBindingSet);
    }

    public static void Unload(GraphicsDevice device)
    {
        device.Destroy(BoxMatBindingSet);
        device.Destroy(CarMatBindingSet);
        device.Destroy(PhongPerFrameBindingSet);
        device.Destroy(PerFrameCb);
        device.Destroy(Container);
        device.Destroy(ContainerSpecular);
        device.Destroy(WhitePixel);
        device.Destroy(PhongVs);
        device.Destroy(PhongPs);
    }
}
