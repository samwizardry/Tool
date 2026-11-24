using Tool.Graphics;

namespace Tool.Assets;

public struct ShaderAsset
{
    public ShaderHandle Handle;
    public ShaderType ShaderType;
}

public struct TextureAsset
{
    public TextureHandle Handle;
    public uint Width;
    public uint Height;
}

public readonly record struct MaterialTextureBinding(
    uint Slot,
    AssetHandle<TextureAsset> Texture);

public readonly record struct MaterialSamplerBinding(
    uint Slot,
    SamplerHandle Sampler);

public readonly record struct MaterialDescription(
    GraphicsPipelineHandle Pipeline,
    BindingLayoutHandle BindingLayout,
    MaterialTextureBinding[]? Textures = null,
    MaterialSamplerBinding[]? Samplers = null,
    BufferDescription? ConstantBufferDesc = null,
    uint ConstantBufferSlot = 0);

public struct MaterialAsset
{
    public GraphicsPipelineHandle Pipeline;

    public BindingLayoutHandle BindingLayout;
    public BindingSetHandle BindingSet;

    public BufferHandle ConstantBuffer;
    public uint ConstantBufferSlot;

    public MaterialTextureBinding[] Textures;
    public MaterialSamplerBinding[] Samplers;

    public bool IsBindingSetDirty;
}

public struct ModelAsset
{
    public Mesh[] Meshes;
    public AssetHandle<MaterialAsset>[] Materials;
}
