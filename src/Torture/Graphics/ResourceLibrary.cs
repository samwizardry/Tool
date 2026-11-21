using Torture.Logging;

namespace Torture.Graphics;

public abstract class ResourceLibrary<TKey, TValue> : IDisposable
    where TKey : notnull
    where TValue : GraphicsResource
{
    protected readonly GraphicsDevice _graphicsDevice;
    protected readonly Dictionary<string, TValue> _resources = [];

    private bool _disposed;

    protected ResourceLibrary(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public TValue this[string key] => _resources[key];

    public TValue? Get(string key)
    {
        if (!_resources.TryGetValue(key, out var resource))
        {
            Log.Error(LogCategories.Core, $"Resource '{key}' not found.");
        }

        return resource;
    }

    public virtual IEnumerable<string> List() => _resources.Keys.AsEnumerable();

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var resource in _resources.Values)
        {
            resource.Dispose();
        }

        _disposed = true;
    }
}
