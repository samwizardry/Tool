using StbImageSharp;

using Torture.Graphics;

namespace Torture.Luna;

public static class ResourceFactoryCache
{
    public static Shader PhongVs = default!;
    public static Shader PhongPs = default!;

    public static InputLayout PhongInputLayout = default!;

    public static RasterState Solid = default!;
    public static RasterState Wireframe = default!;
    public static Sampler Bilinear = default!;
    public static Sampler Anisotropic16 = default!;

    public static Graphics.Buffer PerFrameCb = default!;

    public static Texture WhitePixel = default!;
    public static Texture Container = default!;
    public static Texture ContainerSpecular = default!;

    // Pipelines
    public static GraphicsPipeline PhongPipeline = default!;

    // Binding layouts
    public static BindingLayout PhongBindingLayout = default!;
    public static BindingLayout PhongPerFrameBindingLayout = default!;
    public static BindingLayout PhongPerObjBindingLayout = default!;

    // Material binding sets
    public static BindingSet BoxMatBindingSet = default!;
    public static BindingSet CarMatBindingSet = default!;

    // Per frame binding set
    public static BindingSet PhongPerFrameBindingSet = default!;

    // Materials
    public static Material BoxMaterial = null!;
    public static Material CarMaterial = null!;

    public static void Load(GraphicsDevice device)
    {
        // Shaders
        var shaderSource = File.ReadAllBytes(Path.Combine("Shaders", "Phong.hlsl"));

        PhongVs = device.CreateShader(shaderSource, ShaderDescription.Vertex(
            debug: true,
            debugName: "Triangle_VS"));

        PhongPs = device.CreateShader(shaderSource, ShaderDescription.Pixel(
           debug: true,
           debugName: "Triangle_PS"));

        PhongInputLayout = device.CreateInputLayout(VertexPositionNormalTexture.Layout, PhongVs);

        Solid = device.CreateRasterState(RasterStateDescription.CullBack);
        Wireframe = device.CreateRasterState(RasterStateDescription.Wireframe);
        Bilinear = device.CreateSampler(SamplerDescription.Bilinear);
        Anisotropic16 = device.CreateSampler(SamplerDescription.AnisotropicWrap(16.0f));

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
        PhongPipeline = device.CreateGraphicsPipeline(new GraphicsPipelineDescription(
            PhongVs,
            PhongPs,
            PhongInputLayout,
            Solid,
            PrimitiveTopology.TriangleList));

        // Binding layouts
        PhongBindingLayout = device.CreateBindingLayout(new BindingLayoutDescription(
            Visibility: ShaderType.Vertex | ShaderType.Pixel,
            Bindings: [
                new(0, BindingResourceType.TextureSrv),
                new(1, BindingResourceType.TextureSrv),
                new(0, BindingResourceType.Sampler),
                new(1, BindingResourceType.Sampler)],
            RegisterSpace: 0));

        PhongPerFrameBindingLayout = device.CreateBindingLayout(new BindingLayoutDescription(
            Visibility: ShaderType.Vertex | ShaderType.Pixel,
            Bindings: [
                new(0, BindingResourceType.ConstantBuffer),
            ],
            RegisterSpace: 0));

        PhongPerObjBindingLayout = device.CreateBindingLayout(new BindingLayoutDescription(
            Visibility: ShaderType.Vertex | ShaderType.Pixel,
            Bindings: [
                new(1, BindingResourceType.ConstantBuffer)
            ],
            RegisterSpace: 0));

        // Material binding sets
        BoxMatBindingSet = device.CreateBindingSet(
            desc: new BindingSetDescription(
                Bindings: [
                    new(0, BindingResourceType.TextureSrv, Container),
                    new(1, BindingResourceType.TextureSrv, ContainerSpecular),
                    new(0, BindingResourceType.Sampler, Anisotropic16),
                    new(1, BindingResourceType.Sampler, Anisotropic16)]),
            layout: PhongBindingLayout);

        CarMatBindingSet = device.CreateBindingSet(
            desc: new BindingSetDescription(
                Bindings: [
                    new(0, BindingResourceType.TextureSrv, WhitePixel),
                    new(1, BindingResourceType.TextureSrv, WhitePixel),
                    new(0, BindingResourceType.Sampler, Anisotropic16),
                    new(1, BindingResourceType.Sampler, Anisotropic16)]),
            layout: PhongBindingLayout);

        // Per frame binding set
        PhongPerFrameBindingSet = device.CreateBindingSet(
            desc: new BindingSetDescription(
                Bindings: [
                    new(0, BindingResourceType.ConstantBuffer, PerFrameCb),
                ]),
            layout: PhongPerFrameBindingLayout);

        // Materials
        BoxMaterial = new Material(PhongPipeline, BoxMatBindingSet, null);
        CarMaterial = new Material(PhongPipeline, CarMatBindingSet, null);
    }

    public static void Unload()
    {
        // Shaders, layouts
        PhongVs?.Dispose();
        PhongPs?.Dispose();
        PhongInputLayout?.Dispose();

        // Raster, sampler
        Solid?.Dispose();
        Wireframe?.Dispose();
        Bilinear?.Dispose();
        Anisotropic16?.Dispose();

        // Constant buffers
        PerFrameCb?.Dispose();

        // Textures
        Container?.Dispose();
        ContainerSpecular?.Dispose();
        WhitePixel?.Dispose();

        // Pipelines
        PhongPipeline?.Dispose();

        // Binding layouts
        PhongBindingLayout?.Dispose();
        PhongPerFrameBindingLayout?.Dispose();
        PhongPerObjBindingLayout?.Dispose();

        // Material binding sets
        BoxMatBindingSet?.Dispose();
        CarMatBindingSet?.Dispose();
        // Per frame binding set
        PhongPerFrameBindingSet?.Dispose();

        // Materials
        BoxMaterial?.Dispose();
        CarMaterial?.Dispose();
    }
}
