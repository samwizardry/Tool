using StbImageSharp;

using Torture.Logging;

namespace Torture.Graphics;

public class TextureLibrary : ResourceLibrary<string, Texture>
{
    public TextureLibrary(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
    }

    public Texture? Load(string path)
    {
        if (!_resources.TryGetValue(path, out Texture? texture))
        {
            FileStream? fs = null;

            try
            {
                fs = File.OpenRead(path);
                var image = ImageResult.FromStream(fs, ColorComponents.RedGreenBlueAlpha);

                texture = _graphicsDevice.CreateTexture(
                    image.Data,
                    TextureDescription.Texture2D((uint)image.Width, (uint)image.Height, mipLevels: 0));

                _resources[path] = texture;
            }
            catch (Exception ex)
            {
                Log.Error(LogCategories.Core, $"Failed to load texture '{path}'.", ex);
                texture?.Dispose();
            }
            finally
            {
                fs?.Dispose();
            }
        }
        else
        {
            Log.Warning(LogCategories.Core, $"Texture '{path}' already loaded.");
        }

        return texture;
    }
}
