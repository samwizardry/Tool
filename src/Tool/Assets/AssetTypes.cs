using Tool.Graphics;

namespace Tool.Assets;

#region Shader

public struct ShaderAsset
{
    public ShaderHandle Handle;
    public ShaderType ShaderType;
}

#endregion

#region Texture

public struct TextureAsset
{
    public TextureHandle Handle;
    public uint Width;
    public uint Height;
}

#endregion

#region Material

public readonly record struct MaterialTextureBinding(
    uint Slot,
    AssetHandle<TextureAsset> Texture);

public readonly record struct MaterialSamplerBinding(
    uint Slot,
    SamplerHandle Sampler);

public readonly record struct MaterialDescription(
    GraphicsPipelineHandle Pipeline,
    BindingLayoutHandle BindingLayout,
    BufferDescription? ConstantBufferDesc = null,
    uint ConstantBufferSlot = 0,
    MaterialTextureBinding[]? Textures = null,
    MaterialSamplerBinding[]? Samplers = null);

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

#endregion

#region Model

public struct ModelAsset
{
    public Mesh[] Meshes;
    public AssetHandle<MaterialAsset>[] Materials;
}

#endregion
