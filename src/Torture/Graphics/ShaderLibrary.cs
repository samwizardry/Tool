using Torture.Logging;

namespace Torture.Graphics;

public class ShaderLibrary : ResourceLibrary<string, Shader>
{
    public ShaderLibrary(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
    }

    public Shader? Load(string name, string path, ShaderType type, bool debug = false)
    {
        if (!_resources.TryGetValue(name, out Shader? shader))
        {
            try
            {
                var source = File.ReadAllBytes(path);

                ShaderDescription desc = type switch
                {
                    ShaderType.Vertex => ShaderDescription.Vertex(debug: debug, debugName: name),
                    ShaderType.Pixel => ShaderDescription.Pixel(debug: debug, debugName: name),
                    _ => throw new NotSupportedException()
                };

                shader = _graphicsDevice.CreateShader(source, in desc);
                _resources[name] = shader;
            }
            catch (Exception ex)
            {
                Log.Error(LogCategories.Core, $"Failed to load shader '{name}'.", ex);
                shader?.Dispose();
            }
        }
        else
        {
            Log.Warning(LogCategories.Core, $"Shader '{name}' already loaded.");
        }

        return shader;
    }
}
