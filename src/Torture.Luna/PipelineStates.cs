using StbImageSharp;

using Torture.Graphics;

namespace Torture.Luna;

public static class PipelineStates
{
    public static Shader PhongVs = default!;
    public static Shader PhongPs = default!;
    public static Shader LunaVs = default!;
    public static Shader LunaPs = default!;

    public static InputLayout InputLayout = default!;

    public static RasterState Solid = default!;
    public static RasterState Wireframe = default!;
    public static Sampler Bilinear = default!;
    public static Sampler Anisotropic16 = default!;

    public static Graphics.Buffer PerFrameCb = default!;
    public static Graphics.Buffer PerObjectCb = default!;
    public static Texture Container = default!;
    public static Texture ContainerSpecular = default!;

    public static BindingLayout PhongBindingLayout = default!;
    public static BindingSet PhongBindingSet = default!;
    public static GraphicsPipeline Pipeline1 = default!;

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

        shaderSource = File.ReadAllBytes(Path.Combine("Shaders", "PosNormal.hlsl"));

        LunaVs = device.CreateShader(shaderSource, ShaderDescription.Vertex(
            debug: true,
            debugName: "PosNormal_VS"));

        LunaPs = device.CreateShader(shaderSource, ShaderDescription.Pixel(
            debug: true,
            debugName: "PosNormal_PS"));

        InputLayout = device.CreateInputLayout(VertexPositionNormalTexture.Layout, PhongVs);

        Solid = device.CreateRasterState(RasterStateDescription.CullBack);
        Wireframe = device.CreateRasterState(RasterStateDescription.Wireframe);
        Bilinear = device.CreateSampler(SamplerDescription.Bilinear);
        Anisotropic16 = device.CreateSampler(SamplerDescription.AnisotropicWrap(16.0f));

        PerFrameCb = device.CreateBuffer(BufferDescription.Constant<PerFrameData>());
        PerObjectCb = device.CreateBuffer(BufferDescription.Constant<PerObjectData>());

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

        Pipeline1 = device.CreateGraphicsPipeline(new GraphicsPipelineDescription(
            PhongVs,
            PhongPs,
            InputLayout,
            Solid,
            PrimitiveTopology.TriangleList));

        PhongBindingLayout = device.CreateBindingLayout(new BindingLayoutDescription(
            Visibility: ShaderType.Vertex | ShaderType.Pixel,
            Bindings: [
                new(0, BindingResourceType.ConstantBuffer),
                new(1, BindingResourceType.ConstantBuffer),
                new(0, BindingResourceType.TextureSrv),
                new(1, BindingResourceType.TextureSrv),
                new(0, BindingResourceType.Sampler)
            ],
            RegisterSpace: 0));

        PhongBindingSet = device.CreateBindingSet(
            desc: new BindingSetDescription(
                Bindings: [
                    new(0, BindingResourceType.ConstantBuffer, PerFrameCb),
                    new(1, BindingResourceType.ConstantBuffer, PerObjectCb),
                    new(0, BindingResourceType.TextureSrv, Container),
                    new(1, BindingResourceType.TextureSrv, ContainerSpecular),
                    new(0, BindingResourceType.Sampler, Anisotropic16),
                ]),
            layout: PhongBindingLayout);

        PhongMaterial = new Material(Pipeline1, PhongBindingSet, null);
    }

    public static void Unload()
    {
        PhongVs?.Dispose();
        PhongPs?.Dispose();
        LunaVs?.Dispose();
        LunaPs?.Dispose();
        InputLayout?.Dispose();
        Solid?.Dispose();
        Wireframe?.Dispose();
        Bilinear?.Dispose();
        Anisotropic16?.Dispose();
        PerFrameCb?.Dispose();
        PerObjectCb?.Dispose();
        Container?.Dispose();
        ContainerSpecular?.Dispose();
    }

    public static void SetPerFrameData(GraphicsDevice device, ref PerFrameData data)
    {
        device.UpdateBuffer(PerFrameCb, ref data);
    }
}
