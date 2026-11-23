using Torture.Graphics;
using Torture.Logging;

namespace Torture.Assets;

/// <summary>
/// Central owner of loaded and procedural high-level assets, managing deduplication,
/// reference-counted lifetimes, cascading release, and runtime material updates.
/// </summary>
public sealed class AssetManager : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly string _rootDirectory;

    private readonly AssetPool<ShaderAsset> _shaders = new();
    private readonly AssetPool<TextureAsset> _textures = new();
    private readonly AssetPool<MaterialAsset> _materials = new();
    private readonly AssetPool<ModelAsset> _models = new();

    private IShaderLoader? _shaderLoader;
    private ITextureLoader? _textureLoader;
    private IModelLoader? _modelLoader;

    private bool _disposed;

    public AssetManager(GraphicsDevice device, string rootDirectory = "")
    {
        _device = device;
        _rootDirectory = rootDirectory;
    }

    public string RootDirectory => _rootDirectory;

    public void RegisterShaderLoader(IShaderLoader loader) => _shaderLoader = loader;
    public void RegisterTextureLoader(ITextureLoader loader) => _textureLoader = loader;
    public void RegisterModelLoader(IModelLoader loader) => _modelLoader = loader;

    public string ResolvePath(string virtualPath)
    {
        if (string.IsNullOrEmpty(_rootDirectory) || Path.IsPathRooted(virtualPath))
            return virtualPath;

        return Path.Combine(_rootDirectory, virtualPath);
    }

    #region Acquire & Create

    public AssetHandle<ShaderAsset> AcquireShader(string virtualPath, in ShaderDescription desc)
    {
        // Include stage and entry point because multiple shader stages can live in a single HLSL file.
        AssetId id = AssetId.FromPath($"{virtualPath}#{desc.ShaderType}:{desc.EntryPoint}");
        if (_shaders.TryAcquire(id, out var existing))
            return existing;

        if (_shaderLoader == null)
            throw new InvalidOperationException("No IShaderLoader has been registered with AssetManager.");

        ShaderAsset asset = _shaderLoader.Load(_device, ResolvePath(virtualPath), in desc);
        return _shaders.Add(id, in asset);
    }

    public AssetHandle<TextureAsset> AcquireTexture(string virtualPath, bool generateMips = true)
    {
        AssetId id = AssetId.FromPath(virtualPath);
        if (_textures.TryAcquire(id, out var existing))
            return existing;

        if (_textureLoader == null)
            throw new InvalidOperationException("No ITextureLoader has been registered with AssetManager.");

        TextureAsset asset = _textureLoader.Load(_device, ResolvePath(virtualPath), generateMips);
        return _textures.Add(id, in asset);
    }

    public AssetHandle<TextureAsset> CreateTexture<T>(ReadOnlySpan<T> data, in TextureDescription desc, AssetId id = default)
        where T : unmanaged
    {
        if (id.IsValid && _textures.TryAcquire(id, out var existing))
            return existing;

        TextureHandle handle = _device.CreateTexture(data, in desc);
        var asset = new TextureAsset
        {
            Handle = handle,
            Width = desc.Width,
            Height = desc.Height
        };

        return _textures.Add(id, in asset);
    }

    /// <summary>
    /// Creates a material and retains references to all referenced textures.
    /// </summary>
    public AssetHandle<MaterialAsset> CreateMaterial(in MaterialDescription desc, AssetId id = default)
    {
        if (id.IsValid && _materials.TryAcquire(id, out var existing))
            return existing;

        MaterialTextureBinding[] textures = desc.Textures is { Length: > 0 } ? [.. desc.Textures] : [];
        MaterialSamplerBinding[] samplers = desc.Samplers is { Length: > 0 } ? [.. desc.Samplers] : [];

        // Material retains its own RefCount on every texture it binds.
        foreach (ref readonly var binding in textures.AsSpan())
        {
            if (binding.Texture.IsValid)
            {
                _textures.AddRef(binding.Texture);
            }
        }

        BufferHandle cb = desc.ConstantBufferDesc.HasValue
            ? _device.CreateBuffer(desc.ConstantBufferDesc.Value)
            : BufferHandle.Invalid;

        var mat = new MaterialAsset
        {
            Pipeline = desc.Pipeline,
            BindingLayout = desc.BindingLayout,
            BindingSet = BindingSetHandle.Invalid,
            ConstantBuffer = cb,
            ConstantBufferSlot = desc.ConstantBufferSlot,
            Textures = textures,
            Samplers = samplers,
            IsBindingSetDirty = true
        };

        RebuildMaterialBindingSet(ref mat);
        return _materials.Add(id, in mat);
    }

    public AssetHandle<ModelAsset> AcquireModel(string virtualPath)
    {
        AssetId id = AssetId.FromPath(virtualPath);
        if (_models.TryAcquire(id, out var existing))
            return existing;

        if (_modelLoader == null)
            throw new InvalidOperationException("No IModelLoader has been registered with AssetManager.");

        ModelAsset asset = _modelLoader.Load(_device, this, virtualPath);
        return _models.Add(id, in asset);
    }

    /// <summary>
    /// Creates a procedural model. Ownership of <paramref name="meshes"/> GPU buffers transfers to the model,
    /// and references to <paramref name="materials"/> are retained automatically.
    /// </summary>
    public AssetHandle<ModelAsset> CreateModel(Mesh[] meshes, AssetHandle<MaterialAsset>[] materials, AssetId id = default)
    {
        if (id.IsValid && _models.TryAcquire(id, out var existing))
            return existing;

        AssetHandle<MaterialAsset>[] matCopy = [.. materials];
        foreach (var matHandle in matCopy)
        {
            if (matHandle.IsValid)
            {
                _materials.AddRef(matHandle);
            }
        }

        var asset = new ModelAsset
        {
            Meshes = [.. meshes],
            Materials = matCopy
        };

        return _models.Add(id, in asset);
    }

    #endregion

    #region Access & Runtime Editing

    public ref readonly ShaderAsset Get(AssetHandle<ShaderAsset> handle) => ref _shaders.Get(handle);
    public ref readonly TextureAsset Get(AssetHandle<TextureAsset> handle) => ref _textures.Get(handle);
    public ref readonly ModelAsset Get(AssetHandle<ModelAsset> handle) => ref _models.Get(handle);

    /// <summary>
    /// Returns the material and lazily rebuilds its BindingSet if textures or samplers were edited.
    /// </summary>
    public ref readonly MaterialAsset ResolveMaterial(AssetHandle<MaterialAsset> handle)
    {
        ref MaterialAsset mat = ref _materials.Get(handle);
        if (mat.IsBindingSetDirty)
        {
            RebuildMaterialBindingSet(ref mat);
        }

        return ref mat;
    }

    /// <summary>
    /// Updates per-material scalar/vector constants without recreating the BindingSet.
    /// </summary>
    public void UpdateMaterialConstants<T>(AssetHandle<MaterialAsset> handle, ref T data, uint byteOffset = 0)
        where T : unmanaged
    {
        ref MaterialAsset mat = ref _materials.Get(handle);
        if (mat.ConstantBuffer.IsValid)
        {
            _device.UpdateBuffer(mat.ConstantBuffer, ref data, byteOffset);
        }
    }

    /// <summary>
    /// Replaces or assigns a texture at a shader binding slot and marks the BindingSet dirty.
    /// </summary>
    public void SetMaterialTexture(AssetHandle<MaterialAsset> handle, uint slot, AssetHandle<TextureAsset> newTexture)
    {
        ref MaterialAsset mat = ref _materials.Get(handle);

        for (int i = 0; i < mat.Textures.Length; ++i)
        {
            if (mat.Textures[i].Slot != slot)
                continue;

            AssetHandle<TextureAsset> oldTexture = mat.Textures[i].Texture;
            if (oldTexture == newTexture)
                return;

            // AddRef new BEFORE releasing old to stay safe on self-replacement.
            if (newTexture.IsValid) _textures.AddRef(newTexture);
            if (oldTexture.IsValid) Release(oldTexture);

            mat.Textures[i] = new MaterialTextureBinding(slot, newTexture);
            mat.IsBindingSetDirty = true;
            return;
        }

        if (newTexture.IsValid) _textures.AddRef(newTexture);
        mat.Textures = [.. mat.Textures, new MaterialTextureBinding(slot, newTexture)];
        mat.IsBindingSetDirty = true;
    }

    public void SetMaterialSampler(AssetHandle<MaterialAsset> handle, uint slot, SamplerHandle newSampler)
    {
        ref MaterialAsset mat = ref _materials.Get(handle);

        for (int i = 0; i < mat.Samplers.Length; ++i)
        {
            if (mat.Samplers[i].Slot != slot)
                continue;

            if (mat.Samplers[i].Sampler == newSampler)
                return;

            mat.Samplers[i] = new MaterialSamplerBinding(slot, newSampler);
            mat.IsBindingSetDirty = true;
            return;
        }

        mat.Samplers = [.. mat.Samplers, new MaterialSamplerBinding(slot, newSampler)];
        mat.IsBindingSetDirty = true;
    }

    #endregion

    #region Drawing Helper

    public void DrawModel(
        AssetHandle<ModelAsset> handle,
        BindingSetHandle perFrame = default,
        BindingSetHandle perObject = default)
    {
        ref readonly ModelAsset model = ref _models.Get(handle);
        Mesh[] meshes = model.Meshes;
        AssetHandle<MaterialAsset>[] materials = model.Materials;

        foreach (Mesh mesh in meshes)
        {
            VertexBufferBinding vbBinding = new(mesh.VertexBuffer);
            IndexBufferBinding ibBinding = mesh.IsIndexed
                ? new IndexBufferBinding(mesh.IndexBuffer)
                : default;

            foreach (ref readonly MeshPart part in mesh.Parts)
            {
                ref readonly MaterialAsset material = ref ResolveMaterial(materials[part.MaterialIndex]);

                GraphicsState state = new(
                    Pipeline: material.Pipeline,
                    Bindings: new BindingSetVector
                    {
                        Set0 = perFrame,
                        Set1 = default,
                        Set2 = material.BindingSet,
                        Set3 = perObject
                    },
                    VertexBuffer: vbBinding,
                    IndexBuffer: ibBinding);

                _device.SetGraphicsState(in state);

                if (mesh.IsIndexed)
                {
                    _device.DrawIndexed(part.IndexCount, part.StartIndexLocation, part.BaseVertexLocation);
                }
                else
                {
                    _device.Draw(part.VertexCount, part.StartVertexLocation);
                }
            }
        }
    }

    #endregion

    #region Lifetime & Shutdown

    public void AddRef(AssetHandle<MaterialAsset> handle) => _materials.AddRef(handle);

    public void Release(AssetHandle<ModelAsset> handle)
    {
        if (!_models.TryReleaseRef(handle, out ModelAsset model))
            return;

        foreach (Mesh mesh in model.Meshes)
        {
            mesh?.Destroy(_device);
        }

        foreach (var matHandle in model.Materials)
        {
            if (matHandle.IsValid)
            {
                Release(matHandle);
            }
        }
    }

    public void Release(AssetHandle<MaterialAsset> handle)
    {
        if (!_materials.TryReleaseRef(handle, out MaterialAsset mat))
            return;

        if (mat.BindingSet.IsValid) _device.Destroy(mat.BindingSet);
        if (mat.ConstantBuffer.IsValid) _device.Destroy(mat.ConstantBuffer);

        foreach (ref readonly var binding in mat.Textures.AsSpan())
        {
            if (binding.Texture.IsValid)
            {
                Release(binding.Texture);
            }
        }
    }

    public void Release(AssetHandle<TextureAsset> handle)
    {
        if (!_textures.TryReleaseRef(handle, out TextureAsset tex))
            return;

        if (tex.Handle.IsValid)
        {
            _device.Destroy(tex.Handle);
        }
    }

    public void Release(AssetHandle<ShaderAsset> handle)
    {
        if (!_shaders.TryReleaseRef(handle, out ShaderAsset shader))
            return;

        if (shader.Handle.IsValid)
        {
            _device.Destroy(shader.Handle);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Drain all pools in reverse dependency order and report any unreleased assets in Debug.
        _models.DrainAll((id, handle, model) =>
        {
            Log.Warning(LogCategories.Assets, $"Leaked ModelAsset ({handle.Index}:{handle.Generation}, id={id}) was not released before AssetManager shutdown.");
            foreach (Mesh mesh in model.Meshes)
                mesh?.Destroy(_device);
        });

        _materials.DrainAll((id, handle, mat) =>
        {
            Log.Warning(LogCategories.Assets, $"Leaked MaterialAsset ({handle.Index}:{handle.Generation}, id={id}) was not released before AssetManager shutdown.");
            if (mat.BindingSet.IsValid) _device.Destroy(mat.BindingSet);
            if (mat.ConstantBuffer.IsValid) _device.Destroy(mat.ConstantBuffer);
        });

        _textures.DrainAll((id, handle, tex) =>
        {
            Log.Warning(LogCategories.Assets, $"Leaked TextureAsset ({handle.Index}:{handle.Generation}, id={id}) was not released before AssetManager shutdown.");
            if (tex.Handle.IsValid) _device.Destroy(tex.Handle);
        });

        _shaders.DrainAll((id, handle, shader) =>
        {
            Log.Warning(LogCategories.Assets, $"Leaked ShaderAsset ({handle.Index}:{handle.Generation}, id={id}) was not released before AssetManager shutdown.");
            if (shader.Handle.IsValid) _device.Destroy(shader.Handle);
        });

        _disposed = true;
    }

    #endregion

    private void RebuildMaterialBindingSet(ref MaterialAsset mat)
    {
        if (mat.BindingSet.IsValid)
        {
            _device.Destroy(mat.BindingSet);
            mat.BindingSet = BindingSetHandle.Invalid;
        }

        int count = (mat.ConstantBuffer.IsValid ? 1 : 0) + mat.Textures.Length + mat.Samplers.Length;
        if (count == 0)
        {
            mat.IsBindingSetDirty = false;
            return;
        }

        var items = new BindingSetItem[count];
        int index = 0;

        if (mat.ConstantBuffer.IsValid)
        {
            items[index++] = BindingSetItem.ConstantBuffer(mat.ConstantBufferSlot, mat.ConstantBuffer);
        }

        foreach (ref readonly var tb in mat.Textures.AsSpan())
        {
            TextureHandle gpuTex = _textures.Get(tb.Texture).Handle;
            items[index++] = BindingSetItem.TextureSrv(tb.Slot, gpuTex);
        }

        foreach (ref readonly var sb in mat.Samplers.AsSpan())
        {
            items[index++] = BindingSetItem.Sampler(sb.Slot, sb.Sampler);
        }

        mat.BindingSet = _device.CreateBindingSet(new BindingSetDescription(items), mat.BindingLayout);
        mat.IsBindingSetDirty = false;
    }
}
