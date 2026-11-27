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

public readonly record struct MaterialTextureBinding
{
    public readonly uint Slot;
    public readonly AssetHandle<TextureAsset> Texture;

    public MaterialTextureBinding(uint slot, AssetHandle<TextureAsset> texture)
    {
        Slot = slot;
        Texture = texture;
    }
}

public readonly record struct MaterialSamplerBinding
{
    public readonly uint Slot;
    public readonly SamplerHandle Sampler;

    public MaterialSamplerBinding(uint slot, SamplerHandle sampler)
    {
        Slot = slot;
        Sampler = sampler;
    }
}

public readonly record struct MaterialDescription
{
    public readonly GraphicsPipelineHandle Pipeline;
    public readonly BindingLayoutHandle BindingLayout;
    public readonly BufferDescription? ConstantBufferDesc;
    public readonly uint ConstantBufferSlot;
    public readonly MaterialTextureBinding[]? Textures;
    public readonly MaterialSamplerBinding[]? Samplers;

    public MaterialDescription(
        GraphicsPipelineHandle pipeline,
        BindingLayoutHandle bindingLayout,
        BufferDescription? constantBufferDesc = null,
        uint constantBufferSlot = 0,
        MaterialTextureBinding[]? textures = null,
        MaterialSamplerBinding[]? samplers = null)
    {
        Pipeline = pipeline;
        BindingLayout = bindingLayout;
        ConstantBufferDesc = constantBufferDesc;
        ConstantBufferSlot = constantBufferSlot;
        Textures = textures;
        Samplers = samplers;
    }
}

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
