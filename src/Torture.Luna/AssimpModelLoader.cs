using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Assimp;

using StbImageSharp;

using Torture.Graphics;
using Torture.Logging;

namespace Torture.Luna;

/// <summary>
/// Not thread safe.
/// </summary>
public class AssimpModelLoader : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly AssimpContext _assimpContext;

    private bool _disposed;

    private Scene _scene = null!;
    private Graphics.Mesh[] _meshes = null!;
    private Graphics.Material[] _materials = null!;
    private BindingSet[] _bindingSets = null!;
    private List<Texture> _textures = [];
    private List<Sampler> _samplers = [];

    public AssimpModelLoader(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _assimpContext = new AssimpContext();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _assimpContext.Dispose();
        _scene = null!;
        _meshes = null!;
        _materials = null!;
        _bindingSets = null!;
        _textures = null!;
        _samplers = null!;

        _disposed = true;
    }

    public bool TryLoad(string path,
        [NotNullWhen(true)] out Model? model,
        [NotNullWhen(true)] out Graphics.Material[]? materials,
        [NotNullWhen(true)] out BindingSet[]? bindingSets)
    {
        model = null;
        materials = null;
        bindingSets = null;

        try
        {
            _scene = _assimpContext.ImportFile(path, PostProcessSteps.Triangulate);

            if (_scene == null || (_scene.SceneFlags & SceneFlags.Incomplete) != 0 || _scene.RootNode == null)
            {
                Log.Error(Luna.LogCat, "Failed to load assimp model.");
                return false;
            }

            _meshes = new Graphics.Mesh[_scene.MeshCount];
            _materials = new Graphics.Material[_scene.MaterialCount];
            _bindingSets = new BindingSet[_scene.MaterialCount];

            ProcessNode(_scene.RootNode);
            if (!ProcessMaterials())
            {
                FreeResources();
                return false;
            }

            model = new Model(_meshes, _materials);
            materials = _materials;
            bindingSets = _bindingSets;
        }
        catch (Exception ex)
        {
            Log.Error(Luna.LogCat, "Failed to load assimp model.", ex);
            FreeResources();
            return false;
        }
        finally
        {
            _scene = null!;
            _meshes = null!;
            _materials = null!;
            _bindingSets = null!;
            _textures = [];
            _samplers = [];
        }

        return true;
    }

    private void ProcessNode(Node node)
    {
        foreach (var meshIndex in node.MeshIndices)
        {
            ProcessMesh(_scene.Meshes[meshIndex], meshIndex);
        }

        foreach (var child in node.Children)
        {
            ProcessNode(child);
        }
    }

    private void ProcessMesh(Assimp.Mesh mesh, int meshIndex)
    {
        var vertices = new VertexPositionNormalTexture[mesh.VertexCount];
        uint[] indices = [.. mesh.GetUnsignedIndices()];

        if (mesh.TextureCoordinateChannelCount != 1)
        {
            Log.Warning(Luna.LogCat, $"Model has {mesh.TextureCoordinateChannelCount} texture coordinate channels.");
        }
        if (mesh.HasTangentBasis)
        {
            Log.Warning(Luna.LogCat, $"Model has tangent basis.");
        }
        if (mesh.VertexColorChannelCount > 0)
        {
            Log.Warning(Luna.LogCat, $"Model has color channels.");
        }

        for (int v = 0; v < mesh.VertexCount; v++)
        {
            vertices[v].Position = mesh.Vertices[v];
            vertices[v].Normal = mesh.Normals[v];
            vertices[v].TexCoord = mesh.TextureCoordinateChannels.Length > 0
                ? mesh.TextureCoordinateChannels[0][v].AsVector2()
                : Vector2.Zero;
        }

        var vb = _graphicsDevice.CreateBuffer(vertices, BufferDescription.Vertex(0, 0));
        var ib = _graphicsDevice.CreateBuffer(indices, BufferDescription.Index(0));

        if (mesh.MaterialIndex < 0)
        {
            Log.Warning(Luna.LogCat, $"Negative mesh material index ({mesh.MaterialIndex}).");
        }

        _meshes[meshIndex] = new Graphics.Mesh(vb, ib, [MeshPart.Indexed(ib.Count, materialIndex: mesh.MaterialIndex)]);
    }

    private bool ProcessMaterials()
    {
        if (!_scene.HasMaterials)
        {
            Log.Error(Luna.LogCat, "Scene doesn't have materials.");
            return false;
        }

        foreach (var material in _scene.Materials.Index())
        {
            if (material.Item.Name == "DefaultMaterial")
            {
                Log.Warning(Luna.LogCat, "Model has the DefaultMaterial.");
                continue;
            }

            if (!TryLoadTexturesAndSamplers(material.Item, TextureType.Diffuse, out var diffuseTextures, out var diffuseSamplers))
            {
                Log.Error(Luna.LogCat, "Failed to load diffuse textures.");
                return false;
            }

            if (!TryLoadTexturesAndSamplers(material.Item, TextureType.Specular, out var specularTextures, out var specularSamplers))
            {
                Log.Error(Luna.LogCat, "Failed to load specular textures.");
                return false;
            }

            var bindingSet = _graphicsDevice.CreateBindingSet(
                new BindingSetDescription([
                    new(0, BindingResourceType.TextureSrv, diffuseTextures[0]),
                    new(1, BindingResourceType.TextureSrv, specularTextures[0]),
                    new(0, BindingResourceType.Sampler, diffuseSamplers[0])]
                ),
                ResourceFactoryCache.PhongBindingLayout);

            _bindingSets[material.Index] = bindingSet;
            _materials[material.Index] = new Graphics.Material(ResourceFactoryCache.PhongPipeline, bindingSet, null);
        }

        return true;
    }

    private bool TryLoadTexturesAndSamplers(Assimp.Material material,
        TextureType type,
        [NotNullWhen(true)] out Texture[]? textures,
        [NotNullWhen(true)] out Sampler[]? samplers)
    {
        textures = new Texture[material.GetMaterialTextureCount(type)];
        samplers = new Sampler[textures.Length];

        foreach (var slot in material.GetMaterialTextures(type).Index())
        {
            if (_graphicsDevice.TextureLibrary.Load(Path.Combine("resources/objects/backpack", slot.Item.FilePath))
                is not Texture texture)
            {
                return false;
            }

            _textures.Add(texture);
            textures[slot.Index] = texture;

            var sampler = _graphicsDevice.Factory.GetOrCreateSampler(SamplerDescription.AnisotropicWrap(16.0f) with
            {
                AddressU = ToSamplerAddressMode(slot.Item.WrapModeU),
                AddressV = ToSamplerAddressMode(slot.Item.WrapModeV)
            });

            _samplers.Add(sampler);
            samplers[slot.Index] = sampler;
        }

        return true;
    }

    private void FreeResources()
    {
        foreach (var mesh in _meshes)
        {
            mesh?.Dispose();
        }

        foreach (var material in _materials)
        {
            material?.Dispose();
        }

        foreach (var bindingSet in _bindingSets)
        {
            bindingSet?.Dispose();
        }

        foreach (var texture in _textures ?? Enumerable.Empty<Texture>())
        {
            // TBD: call texture library to free that texture
        }

        foreach (var texture in _samplers ?? Enumerable.Empty<Sampler>())
        {
            // TBD: call texture library to free that texture
        }
    }

    private static SamplerAddressMode ToSamplerAddressMode(TextureWrapMode wrapMode) =>
        wrapMode switch
        {
            TextureWrapMode.Wrap => SamplerAddressMode.Wrap,
            TextureWrapMode.Clamp => SamplerAddressMode.Clamp,
            _ => throw new NotSupportedException()
        };
}
