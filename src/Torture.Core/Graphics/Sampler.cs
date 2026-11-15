namespace Torture.Core.Graphics;

/// <summary>
/// Represents a GPU texture sampler state.
/// </summary>
public abstract class Sampler : IDisposable
{
    protected readonly SamplerDescription _desc;
    protected bool _disposed;

    protected Sampler(in SamplerDescription desc)
    {
        _desc = desc;
    }

    public ref readonly SamplerDescription Desc => ref _desc;

    public abstract void Dispose();
}
