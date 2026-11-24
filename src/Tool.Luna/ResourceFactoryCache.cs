using Tool.Assets;
using Tool.Graphics;

namespace Tool.Luna;

public static class ResourceFactoryCache
{
    public static AssetHandle<ShaderAsset> PhongVs;
    public static AssetHandle<ShaderAsset> PhongPs;

    public static InputLayoutHandle PhongInputLayout;

    public static RasterStateHandle Solid;
    public static RasterStateHandle Wireframe;
    public static SamplerHandle Bilinear;
    public static SamplerHandle Anisotropic16;

    public static BufferHandle PerFrameCb;

    public static AssetHandle<TextureAsset> WhitePixel;
    public static AssetHandle<TextureAsset> Container;
    public static AssetHandle<TextureAsset> ContainerSpecular;

    // Binding layouts
    public static BindingLayoutHandle PhongBindingLayout;
    public static BindingLayoutHandle PhongPerFrameBindingLayout;
    public static BindingLayoutHandle PhongPerObjBindingLayout;

    // Pipelines
    public static GraphicsPipelineHandle PhongPipeline;

    // Per frame binding set
    public static BindingSetHandle PhongPerFrameBindingSet;

    // Materials
    public static AssetHandle<MaterialAsset> BoxMaterial;
    public static AssetHandle<MaterialAsset> CarMaterial;


    public static void Load(GraphicsDevice device, AssetScope scope)
    {
        // Shaders
        PhongVs = scope.LoadShader("Shaders/Phong.hlsl", ShaderDescription.Vertex(debug: true, debugName: "phong.vs"));
        PhongPs = scope.LoadShader("Shaders/Phong.hlsl", ShaderDescription.Pixel(debug: true, debugName: "phong.ps"));

        var phongVs = scope.Get(PhongVs).Handle;
        var phongPs = scope.Get(PhongPs).Handle;

        // Input layouts, raster states, samplers
        PhongInputLayout = device.GetOrCreateInputLayout(VertexPositionNormalTexture.Layout, phongVs);

        Solid = device.GetOrCreateRasterState(RasterStateDescription.CullBack);
        Wireframe = device.GetOrCreateRasterState(RasterStateDescription.Wireframe);

        Bilinear = device.GetOrCreateSampler(SamplerDescription.Bilinear);
        Anisotropic16 = device.GetOrCreateSampler(SamplerDescription.AnisotropicWrap(16.0f));

        PerFrameCb = device.CreateBuffer(BufferDescription.Constant<PhongPerFrame>());

        // Textures
        WhitePixel = scope.CreateTexture(new byte[] { 255, 255, 255, 255 }, TextureDescription.Texture2D(1u, 1u));
        Container = scope.LoadTexture("resources/textures/container2.png");
        ContainerSpecular = scope.LoadTexture("resources/textures/container2_specular.png");

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

        // Pipelines
        PhongPipeline = device.GetOrCreateGraphicsPipeline(new GraphicsPipelineDescription(
            VertexShader: phongVs,
            PixelShader: phongPs,
            InputLayout: PhongInputLayout,
            BindingLayouts: new(PhongPerFrameBindingLayout, default, PhongBindingLayout, PhongPerObjBindingLayout),
            RasterState: Solid,
            Topology: PrimitiveTopology.TriangleList));

        // Per frame binding set
        PhongPerFrameBindingSet = device.CreateBindingSet(
            desc: new BindingSetDescription(
                Bindings: [
                    BindingSetItem.ConstantBuffer(0, PerFrameCb)
                ]),
            layout: PhongPerFrameBindingLayout);

        // Materials
        BoxMaterial = scope.CreateMaterial(new MaterialDescription(PhongPipeline,
            PhongBindingLayout,
            [new(0, Container), new(1, ContainerSpecular)],
            [new(0, Anisotropic16)]));

        CarMaterial = scope.CreateMaterial(new MaterialDescription(PhongPipeline,
            PhongBindingLayout,
            [new(0, WhitePixel), new(1, WhitePixel)],
            [new(0, Anisotropic16)]));
    }

    public static void Unload(GraphicsDevice device)
    {
        device.Destroy(PhongPerFrameBindingSet);
        device.Destroy(PerFrameCb);
    }
}
