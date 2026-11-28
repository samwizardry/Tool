using System.Runtime.InteropServices;

using Tool.Graphics;

namespace Tool.Assets;

/// <summary>
/// Ownership boundary (e.g. Global, Scene, or Streaming Chunk) that tracks acquired assets
/// and releases them deterministically in reverse dependency order upon disposal.
/// </summary>
public sealed class AssetScope : IDisposable
{
    private readonly List<AssetHandle<ShaderAsset>> _shaders = [];
    private readonly List<AssetHandle<TextureAsset>> _textures = [];
    private readonly List<AssetHandle<MaterialAsset>> _materials = [];
    private readonly List<AssetHandle<ModelAsset>> _models = [];

    private bool _disposed;

    public AssetScope(AssetManager manager)
    {
        Manager = manager;
    }

    public AssetManager Manager { get; }

    public AssetHandle<ShaderAsset> LoadShader(string virtualPath, in ShaderDescription desc)
    {
        var handle = Manager.AcquireShader(virtualPath, in desc);
        _shaders.Add(handle);
        return handle;
    }

    public AssetHandle<TextureAsset> LoadTexture(string virtualPath, bool generateMips = true)
    {
        var handle = Manager.AcquireTexture(virtualPath, generateMips);
        _textures.Add(handle);
        return handle;
    }

    public AssetHandle<TextureAsset> CreateTexture<T>(ReadOnlySpan<T> data, in TextureDescription desc, AssetId id = default)
        where T : unmanaged
    {
        var handle = Manager.CreateTexture(data, in desc, id);
        _textures.Add(handle);
        return handle;
    }

    public AssetHandle<MaterialAsset> CreateMaterial(in MaterialDescription desc, AssetId id = default)
    {
        var handle = Manager.CreateMaterial(in desc, id);
        _materials.Add(handle);
        return handle;
    }

    public AssetHandle<ModelAsset> LoadModel(string virtualPath)
    {
        var handle = Manager.AcquireModel(virtualPath);
        _models.Add(handle);
        return handle;
    }

    public AssetHandle<ModelAsset> CreateModel(Mesh[] meshes, AssetHandle<MaterialAsset>[] materials, AssetId id = default)
    {
        var handle = Manager.CreateModel(meshes, materials, id);
        _models.Add(handle);
        return handle;
    }

    public AssetHandle<ModelAsset> CreateModel(Mesh mesh, AssetHandle<MaterialAsset> material, AssetId id = default) =>
        CreateModel([mesh], [material], id);

    public ShaderAsset Get(AssetHandle<ShaderAsset> handle) => Manager.Get(handle);
    public ref readonly TextureAsset Get(AssetHandle<TextureAsset> handle) => ref Manager.Get(handle);
    public ref readonly ModelAsset Get(AssetHandle<ModelAsset> handle) => ref Manager.Get(handle);
    public ref readonly MaterialAsset ResolveMaterial(AssetHandle<MaterialAsset> handle) => ref Manager.ResolveMaterial(handle);

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var h in CollectionsMarshal.AsSpan(_models)) Manager.Release(h);
        foreach (var h in CollectionsMarshal.AsSpan(_materials)) Manager.Release(h);
        foreach (var h in CollectionsMarshal.AsSpan(_textures)) Manager.Release(h);
        foreach (var h in CollectionsMarshal.AsSpan(_shaders)) Manager.Release(h);

        _models.Clear();
        _materials.Clear();
        _textures.Clear();
        _shaders.Clear();

        _disposed = true;
    }
}
