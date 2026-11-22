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
    private readonly GraphicsDevice _device;
    private readonly AssimpContext _context;

    private bool _disposed;

    private Scene _scene = null!;
    private Graphics.Mesh[] _meshes = null!;
    private Graphics.Material[] _materials = null!;
    private BindingSetHandle[] _bindingSets = null!;
    private List<TextureHandle> _textures = [];

    public AssimpModelLoader(GraphicsDevice device)
    {
        _device = device;
        _context = new AssimpContext();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _context.Dispose();
        _scene = null!;
        _meshes = null!;
        _materials = null!;
        _bindingSets = null!;
        _textures = null!;

        _disposed = true;
    }

    public bool TryLoad(string path,
        [NotNullWhen(true)] out Model? model,
        [NotNullWhen(true)] out Graphics.Material[]? materials,
        [NotNullWhen(true)] out BindingSetHandle[]? bindingSets)
    {
        model = null;
        materials = null;
        bindingSets = null;

        try
        {
            _scene = _context.ImportFile(path, PostProcessSteps.Triangulate);

            if (_scene == null || (_scene.SceneFlags & SceneFlags.Incomplete) != 0 || _scene.RootNode == null)
            {
                Log.Error(Luna.LogCat, "Failed to load assimp model.");
                return false;
            }

            _meshes = new Graphics.Mesh[_scene.MeshCount];
            _materials = new Graphics.Material[_scene.MaterialCount];
            _bindingSets = new BindingSetHandle[_scene.MaterialCount];

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

        var vb = _device.CreateBuffer(vertices, BufferDescription.Vertex(0, 0));
        var ib = _device.CreateBuffer(indices, BufferDescription.Index(0));

        if (mesh.MaterialIndex < 0)
        {
            Log.Warning(Luna.LogCat, $"Negative mesh material index ({mesh.MaterialIndex}).");
        }

        _meshes[meshIndex] = new Graphics.Mesh(vb, ib, [MeshPart.Indexed(_device.GetBufferDescription(ib).Count, materialIndex: mesh.MaterialIndex)]);
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

            var bindingSet = _device.CreateBindingSet(
                new BindingSetDescription([
                    BindingSetItem.TextureSrv(0, diffuseTextures[0]),
                    BindingSetItem.TextureSrv(1, specularTextures[0]),
                    BindingSetItem.Sampler(0, diffuseSamplers[0])]
                ),
                ResourceFactoryCache.PhongBindingLayout);

            _bindingSets[material.Index] = bindingSet;
            _materials[material.Index] = new Graphics.Material(ResourceFactoryCache.PhongPipeline, bindingSet);
        }

        return true;
    }

    private bool TryLoadTexturesAndSamplers(Assimp.Material material,
        TextureType type,
        [NotNullWhen(true)] out TextureHandle[]? textures,
        [NotNullWhen(true)] out SamplerHandle[]? samplers)
    {
        textures = new TextureHandle[material.GetMaterialTextureCount(type)];
        samplers = new SamplerHandle[textures.Length];

        foreach (var slot in material.GetMaterialTextures(type).Index())
        {
            using (var fs = File.OpenRead(Path.Combine("resources/objects/backpack", slot.Item.FilePath)))
            {
                var image = ImageResult.FromStream(fs, ColorComponents.RedGreenBlueAlpha);
                var texture = _device.CreateTexture(image.Data, TextureDescription.Texture2D((uint)image.Width, (uint)image.Height));
                textures[slot.Index] = texture;
            }

            samplers[slot.Index] = _device.GetOrCreateSampler(SamplerDescription.AnisotropicWrap(16.0f) with
            {
                AddressU = ToSamplerAddressMode(slot.Item.WrapModeU),
                AddressV = ToSamplerAddressMode(slot.Item.WrapModeV)
            });
        }

        return true;
    }

    private void FreeResources()
    {
        foreach (var mesh in _meshes)
        {
            mesh?.Destroy(_device);
        }

        foreach (var bindingSet in _bindingSets)
        {
            _device.Destroy(bindingSet);
        }

        foreach (var texture in _textures ?? Enumerable.Empty<TextureHandle>())
        {
            _device.Destroy(texture);
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
