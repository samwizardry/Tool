using System.Numerics;

using Assimp;

using Tool.Assets;
using Tool.Graphics;
using Tool.Logging;

namespace Tool.Luna;

public sealed class AssimpModelLoader : IModelLoader
{
    public ModelAsset Load(GraphicsDevice device, AssetManager assets, string virtualPath)
    {
        string fullPath = assets.ResolvePath(virtualPath);
        string virtualDir = Path.GetDirectoryName(virtualPath) ?? string.Empty;

        using var context = new AssimpContext();
        // Temporary scope holds loader's local texture refs and releases them automatically
        // on exit (both on success and on exception), leaving only MaterialAsset's retained refs alive.
        using var tempTextures = new AssetScope(assets);

        Scene? scene = context.ImportFile(fullPath, PostProcessSteps.Triangulate);
        if (scene == null || (scene.SceneFlags & SceneFlags.Incomplete) != 0 || scene.RootNode == null)
        {
            throw new InvalidDataException($"Failed to import model '{virtualPath}' with Assimp.");
        }

        var meshes = new Graphics.Mesh[scene.MeshCount];
        var materials = new AssetHandle<MaterialAsset>[scene.MaterialCount];

        try
        {
            ProcessNode(device, scene, scene.RootNode, meshes);
            ProcessMaterials(device, assets, tempTextures, scene, virtualDir, materials);

            return new ModelAsset
            {
                Meshes = meshes,
                Materials = materials
            };
        }
        catch
        {
            foreach (Graphics.Mesh? mesh in meshes)
            {
                mesh?.Destroy(device);
            }

            foreach (var mat in materials)
            {
                if (mat.IsValid)
                {
                    assets.Release(mat);
                }
            }

            throw;
        }
    }

    private static void ProcessNode(GraphicsDevice device, Scene scene, Node node, Graphics.Mesh[] meshes)
    {
        foreach (int meshIndex in node.MeshIndices)
        {
            meshes[meshIndex] = ProcessMesh(device, scene.Meshes[meshIndex]);
        }

        foreach (Node child in node.Children)
        {
            ProcessNode(device, scene, child, meshes);
        }
    }

    private static Graphics.Mesh ProcessMesh(GraphicsDevice device, Assimp.Mesh mesh)
    {
        var vertices = new VertexPositionNormalTexture[mesh.VertexCount];
        uint[] indices = [.. mesh.GetUnsignedIndices()];

        if (mesh.TextureCoordinateChannelCount != 1)
        {
            Log.Warning(Luna.LogCat, $"Model has {mesh.TextureCoordinateChannelCount} texture coordinate channels.");
        }
        if (mesh.HasTangentBasis)
        {
            Log.Warning(Luna.LogCat, "Model has tangent basis.");
        }
        if (mesh.VertexColorChannelCount > 0)
        {
            Log.Warning(Luna.LogCat, "Model has color channels.");
        }

        for (int v = 0; v < mesh.VertexCount; v++)
        {
            vertices[v].Position = mesh.Vertices[v];
            vertices[v].Normal = mesh.Normals[v];
            vertices[v].TexCoord = mesh.TextureCoordinateChannels.Length > 0
                ? mesh.TextureCoordinateChannels[0][v].AsVector2()
                : Vector2.Zero;
        }

        BufferHandle vb = device.CreateBuffer(vertices.AsSpan(), BufferDescription.Vertex(0, 0));
        BufferHandle ib = device.CreateBuffer(indices.AsSpan(), BufferDescription.Index(0));

        if (mesh.MaterialIndex < 0)
        {
            Log.Warning(Luna.LogCat, $"Negative mesh material index ({mesh.MaterialIndex}).");
        }

        return new Graphics.Mesh(
            vb,
            ib,
            IndexFormat.UInt32,
            [MeshPart.Indexed(device.GetBufferDescription(ib).Count, materialIndex: mesh.MaterialIndex)]);
    }

    private static void ProcessMaterials(
        GraphicsDevice device,
        AssetManager assets,
        AssetScope tempTextures,
        Scene scene,
        string virtualDir,
        AssetHandle<MaterialAsset>[] materials)
    {
        if (!scene.HasMaterials)
        {
            throw new InvalidDataException("Model scene does not contain any materials.");
        }

        foreach (var (index, assimpMaterial) in scene.Materials.Index())
        {
            if (assimpMaterial.Name == "DefaultMaterial")
            {
                Log.Warning(Luna.LogCat, "Model has the DefaultMaterial.");
                continue;
            }

            LoadTexturesAndSamplers(device, tempTextures, assimpMaterial, TextureType.Diffuse, virtualDir, out var diffuseTextures, out var diffuseSamplers);
            LoadTexturesAndSamplers(device, tempTextures, assimpMaterial, TextureType.Specular, virtualDir, out var specularTextures, out var specularSamplers);

            if (diffuseTextures.Length == 0 || specularTextures.Length == 0)
            {
                throw new InvalidDataException($"Material '{assimpMaterial.Name}' is missing diffuse or specular texture.");
            }

            materials[index] = assets.CreateMaterial(new MaterialDescription(
                Pipeline: Phong.Pipeline,
                BindingLayout: Phong.MatBL,
                Textures: [new(0, diffuseTextures[0]), new(1, specularTextures[0])],
                Samplers: [new(0, Resources.Anisotropic16)]
            ));
        }
    }

    private static void LoadTexturesAndSamplers(
        GraphicsDevice device,
        AssetScope tempTextures,
        Material material,
        TextureType type,
        string virtualDir,
        out AssetHandle<TextureAsset>[] textures,
        out SamplerHandle[] samplers)
    {
        int count = material.GetMaterialTextureCount(type);
        textures = new AssetHandle<TextureAsset>[count];
        samplers = new SamplerHandle[count];

        foreach (var (slotIndex, slot) in material.GetMaterialTextures(type).Index())
        {
            string texVirtualPath = Path.Combine(virtualDir, slot.FilePath);
            textures[slotIndex] = tempTextures.LoadTexture(texVirtualPath, generateMips: true);

            samplers[slotIndex] = device.GetOrCreateSampler(SamplerDescription.AnisotropicWrap(16.0f) with
            {
                AddressU = ToSamplerAddressMode(slot.WrapModeU),
                AddressV = ToSamplerAddressMode(slot.WrapModeV)
            });
        }
    }

    private static SamplerAddressMode ToSamplerAddressMode(TextureWrapMode wrapMode) =>
        wrapMode switch
        {
            TextureWrapMode.Wrap => SamplerAddressMode.Wrap,
            TextureWrapMode.Clamp => SamplerAddressMode.Clamp,
            _ => throw new NotSupportedException($"Unsupported texture wrap mode: {wrapMode}.")
        };
}