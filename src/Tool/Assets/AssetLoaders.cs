using Tool.Graphics;

namespace Tool.Assets;

public interface IShaderLoader
{
    ShaderAsset Load(GraphicsDevice device, string virtualPath, in ShaderDescription desc);
}

public interface ITextureLoader
{
    TextureAsset Load(GraphicsDevice device, string virtualPath, bool generateMips = true);
}

public interface IModelLoader
{
    ModelAsset Load(GraphicsDevice device, AssetManager assets, string virtualPath);
}
