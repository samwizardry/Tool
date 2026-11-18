namespace Torture.Graphics;

public abstract class Shader : IDisposable
{
    private readonly ShaderDescription _desc;
    protected bool _disposed;

    protected Shader(in ShaderDescription desc)
    {
        _desc = desc;
    }

    public ref readonly ShaderDescription Desc => ref _desc;

    public ShaderType ShaderType => _desc.ShaderType;

    public abstract void Dispose();
}
