using StbImageSharp;

using Tool.Assets;
using Tool.Graphics;

namespace Tool.Luna;

public sealed class FileShaderLoader : IShaderLoader
{
    public ShaderAsset Load(GraphicsDevice device, string path, in ShaderDescription desc)
    {
        byte[] source = File.ReadAllBytes(path);
        ShaderHandle handle = device.CreateShader(source, in desc);
        return new ShaderAsset
        {
            Handle = handle,
            ShaderType = desc.ShaderType
        };
    }
}
public sealed class StbTextureLoader : ITextureLoader
{
    public TextureAsset Load(GraphicsDevice device, string path, bool generateMips = true)
    {
        using FileStream fs = File.OpenRead(path);
        ImageResult image = ImageResult.FromStream(fs, ColorComponents.RedGreenBlueAlpha);

        uint width = (uint)image.Width;
        uint height = (uint)image.Height;
        uint mipLevels = generateMips ? 0u : 1u;

        TextureHandle handle = device.CreateTexture(
            image.Data.AsSpan(),
            TextureDescription.Texture2D(width, height, mipLevels: mipLevels));

        return new TextureAsset
        {
            Handle = handle,
            Width = width,
            Height = height
        };
    }
}
