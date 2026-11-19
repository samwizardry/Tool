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
    public static Graphics.Buffer PerObjectCb = default!;

    public static Texture WhitePixel = default!;
    public static Texture Container = default!;
    public static Texture ContainerSpecular = default!;

    public static BindingLayout MatBindingLayout = default!;
    public static BindingSet MatBindingSet = default!;

    public static BindingLayout PerFrameBindingLayout = default!;
    public static BindingSet PerFrameBindingSet = default!;

    public static BindingLayout PerObjBindingLayout = default!;
    public static BindingSet PerObjBindingSet = default!;

    public static GraphicsPipeline PhongPipeline = default!;

    public static Material PhongMaterial = null!;

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

        PerFrameCb = device.CreateBuffer(BufferDescription.Constant<PerFrameData>());
        PerObjectCb = device.CreateBuffer(BufferDescription.Constant<PerObjectData>());

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

        PhongPipeline = device.CreateGraphicsPipeline(new GraphicsPipelineDescription(
            PhongVs,
            PhongPs,
            PhongInputLayout,
            Solid,
            PrimitiveTopology.TriangleList));

        MatBindingLayout = device.CreateBindingLayout(new BindingLayoutDescription(
            Visibility: ShaderType.Vertex | ShaderType.Pixel,
            Bindings: [],
            RegisterSpace: 0));

        MatBindingSet = device.CreateBindingSet(
            desc: new BindingSetDescription(
                Bindings: []),
            layout: MatBindingLayout);

        PerFrameBindingLayout = device.CreateBindingLayout(new BindingLayoutDescription(
            Visibility: ShaderType.Vertex | ShaderType.Pixel,
            Bindings: [
                new(0, BindingResourceType.ConstantBuffer),
            ],
            RegisterSpace: 0));

        PerFrameBindingSet = device.CreateBindingSet(
            desc: new BindingSetDescription(
                Bindings: [
                    new(0, BindingResourceType.ConstantBuffer, PerFrameCb),
                ]),
            layout: PerFrameBindingLayout);

        PerObjBindingLayout = device.CreateBindingLayout(new BindingLayoutDescription(
            Visibility: ShaderType.Vertex | ShaderType.Pixel,
            Bindings: [
                new(1, BindingResourceType.ConstantBuffer),
                new(0, BindingResourceType.TextureSrv),
                new(1, BindingResourceType.TextureSrv),
                new(0, BindingResourceType.Sampler)
            ],
            RegisterSpace: 0));

        PerObjBindingSet = device.CreateBindingSet(
            desc: new BindingSetDescription(
                Bindings: [
                    new(1, BindingResourceType.ConstantBuffer, PerObjectCb),
                    new(0, BindingResourceType.TextureSrv, Container),
                    new(1, BindingResourceType.TextureSrv, ContainerSpecular),
                    new(0, BindingResourceType.Sampler, Anisotropic16),
                ]),
            layout: PerObjBindingLayout);

        PhongMaterial = new Material(PhongPipeline, MatBindingSet, null);
    }

    public static void Unload()
    {
        PhongVs?.Dispose();
        PhongPs?.Dispose();
        PhongInputLayout?.Dispose();

        Solid?.Dispose();
        Wireframe?.Dispose();
        Bilinear?.Dispose();
        Anisotropic16?.Dispose();

        PerFrameCb?.Dispose();
        PerObjectCb?.Dispose();

        Container?.Dispose();
        ContainerSpecular?.Dispose();

        MatBindingLayout?.Dispose();
        MatBindingSet?.Dispose();
        PerFrameBindingLayout?.Dispose();
        PerFrameBindingSet?.Dispose();
        PerObjBindingLayout?.Dispose();
        PerObjBindingSet?.Dispose();
        PhongPipeline?.Dispose();
        PhongMaterial?.Dispose();
    }

    public static void SetPerFrameData(GraphicsDevice device, ref PerFrameData data)
    {
        device.UpdateBuffer(PerFrameCb, ref data);
    }
}
