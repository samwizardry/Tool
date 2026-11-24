using Tool.Graphics;

namespace Tool.Assets;

/// <summary>
/// Ownership boundary (e.g. Global, Scene, or Streaming Chunk) that tracks acquired assets
/// and releases them deterministically in reverse dependency order upon disposal.
/// </summary>
public sealed class AssetScope : IDisposable
{
    private readonly AssetManager _assets;

    private readonly List<AssetHandle<ShaderAsset>> _shaders = [];
    private readonly List<AssetHandle<TextureAsset>> _textures = [];
    private readonly List<AssetHandle<MaterialAsset>> _materials = [];
    private readonly List<AssetHandle<ModelAsset>> _models = [];

    private bool _disposed;

    public AssetScope(AssetManager assets)
    {
        _assets = assets;
    }

    public AssetManager Manager => _assets;

    public AssetHandle<ShaderAsset> LoadShader(string virtualPath, in ShaderDescription desc)
    {
        var handle = _assets.AcquireShader(virtualPath, in desc);
        _shaders.Add(handle);
        return handle;
    }

    public AssetHandle<TextureAsset> LoadTexture(string virtualPath, bool generateMips = true)
    {
        var handle = _assets.AcquireTexture(virtualPath, generateMips);
        _textures.Add(handle);
        return handle;
    }

    public AssetHandle<TextureAsset> CreateTexture<T>(ReadOnlySpan<T> data, in TextureDescription desc, AssetId id = default)
        where T : unmanaged
    {
        var handle = _assets.CreateTexture(data, in desc, id);
        _textures.Add(handle);
        return handle;
    }

    public AssetHandle<MaterialAsset> CreateMaterial(in MaterialDescription desc, AssetId id = default)
    {
        var handle = _assets.CreateMaterial(in desc, id);
        _materials.Add(handle);
        return handle;
    }

    public AssetHandle<ModelAsset> LoadModel(string virtualPath)
    {
        var handle = _assets.AcquireModel(virtualPath);
        _models.Add(handle);
        return handle;
    }

    public AssetHandle<ModelAsset> CreateModel(Mesh[] meshes, AssetHandle<MaterialAsset>[] materials, AssetId id = default)
    {
        var handle = _assets.CreateModel(meshes, materials, id);
        _models.Add(handle);
        return handle;
    }

    public AssetHandle<ModelAsset> CreateModel(Mesh mesh, AssetHandle<MaterialAsset> material, AssetId id = default) =>
        CreateModel([mesh], [material], id);

    public ref readonly ShaderAsset Get(AssetHandle<ShaderAsset> handle) => ref _assets.Get(handle);
    public ref readonly TextureAsset Get(AssetHandle<TextureAsset> handle) => ref _assets.Get(handle);
    public ref readonly ModelAsset Get(AssetHandle<ModelAsset> handle) => ref _assets.Get(handle);
    public ref readonly MaterialAsset ResolveMaterial(AssetHandle<MaterialAsset> handle) => ref _assets.ResolveMaterial(handle);

    public void Dispose()
    {
        if (_disposed)
            return;

        // Release in reverse dependency order: Models -> Materials -> Textures -> Shaders
        foreach (var h in _models) _assets.Release(h);
        foreach (var h in _materials) _assets.Release(h);
        foreach (var h in _textures) _assets.Release(h);
        foreach (var h in _shaders) _assets.Release(h);

        _models.Clear();
        _materials.Clear();
        _textures.Clear();
        _shaders.Clear();

        _disposed = true;
    }
}
