using System.Numerics;

using Assimp;

using StbImageSharp;

using Torture.Graphics;
using Torture.Logging;

namespace Torture.Luna;

public static class AssimpModelLoader
{
    public static GraphicsDevice GraphicsDevice = null!;
    public static Graphics.Mesh[] Meshes = null!;
    public static Graphics.Material[] Materials = null!;

    public static BindingSet[] BindingSets = null!;

    public static List<Texture> DiffuseTextures = [];
    public static List<Sampler> DiffuseSamplers = [];
    public static List<Texture> SpecularTextures = [];
    public static List<Sampler> SpecularSamplers = [];

    private static int MeshCount;

    public struct TempMesh
    {
        public VertexPositionNormalTexture[] Vertices;
        public uint[] Indices;
        public List<TextureDescription> DiffuseMaps;
        public List<byte[]> DiffuseMapsData;
        public List<SamplerDescription> DiffuseSamplers;
        public List<TextureDescription> SpecularMaps;
        public List<byte[]> SpecularMapsData;
        public List<SamplerDescription> SpecularSamplers;
    }

    public static Model? LoadFromFile(string path)
    {
        using var ctx = new AssimpContext();
        ctx.GetSupportedImportFormats();

        var scene = ctx.ImportFile(path, PostProcessSteps.Triangulate);

        if (scene == null || (scene.SceneFlags & SceneFlags.Incomplete) != 0 || scene.RootNode == null)
        {
            Log.Error(Luna.LogCat, "Error loading model.");
            return null;
        }

        Meshes = new Graphics.Mesh[scene.MeshCount];
        Materials = new Graphics.Material[scene.MaterialCount];
        BindingSets = new BindingSet[scene.MaterialCount];

        ProcessNode(scene.RootNode, scene);

        return new Model(Meshes, Materials);
    }

    private static void ProcessNode(Node node, Scene scene)
    {
        foreach (var meshIndex in node.MeshIndices)
        {
            ProcessMesh(scene.Meshes[meshIndex], scene);
        }

        for (int i = 0; i < node.ChildCount; i++)
        {
            ProcessNode(node.Children[i], scene);
        }
    }

    private static void ProcessMesh(Assimp.Mesh mesh, Scene scene)
    {
        TempMesh tm;
        tm.Vertices = new VertexPositionNormalTexture[mesh.VertexCount];
        tm.Indices = [.. mesh.GetUnsignedIndices()];
        tm.DiffuseMaps = [];
        tm.DiffuseMapsData = [];
        tm.DiffuseSamplers = [];
        tm.SpecularMaps = [];
        tm.SpecularMapsData = [];
        tm.SpecularSamplers = [];

        for (int v = 0; v < mesh.VertexCount; v++)
        {
            tm.Vertices[v].Position = mesh.Vertices[v];
            tm.Vertices[v].Normal = mesh.Normals[v];
            tm.Vertices[v].TexCoord = mesh.TextureCoordinateChannels.Length > 0
                ? mesh.TextureCoordinateChannels[0][v].AsVector2()
                : Vector2.Zero;
        }

        var vb = GraphicsDevice.CreateBuffer(tm.Vertices, BufferDescription.Vertex(0, 0));
        var ib = GraphicsDevice.CreateBuffer(tm.Indices, BufferDescription.Index(0));

        if (mesh.MaterialIndex < 0)
        {
            Log.Warning(Luna.LogCat, "Material index < 0");
        }

        Meshes[MeshCount++] = new Graphics.Mesh(
            vb,
            ib,
            [MeshPart.Indexed(ib.Count, materialIndex: mesh.MaterialIndex)]
        );

        if (scene.HasMaterials && mesh.MaterialIndex >= 0 && Materials[mesh.MaterialIndex] == null)
        {
            var material = scene.Materials[mesh.MaterialIndex];
            LoadTextures(material, TextureType.Diffuse, tm.DiffuseMaps, tm.DiffuseMapsData, tm.DiffuseSamplers);
            LoadTextures(material, TextureType.Specular, tm.SpecularMaps, tm.SpecularMapsData, tm.SpecularSamplers);

            Texture tex = default!;
            Sampler samp = default!;
            Texture tex1 = default!;
            Sampler samp1 = default!;

            for (int i = 0; i < tm.DiffuseMaps.Count; i++)
            {
                tex = GraphicsDevice.CreateTexture(tm.DiffuseMapsData[i], tm.DiffuseMaps[i]);
                DiffuseTextures.Add(tex);

                samp = GraphicsDevice.CreateSampler(tm.DiffuseSamplers[i]);
                DiffuseSamplers.Add(samp);
            }

            for (int i = 0; i < tm.SpecularMaps.Count; i++)
            {
                tex1 = GraphicsDevice.CreateTexture(tm.SpecularMapsData[i], tm.SpecularMaps[i]);
                SpecularTextures.Add(tex1);

                samp1 = GraphicsDevice.CreateSampler(tm.SpecularSamplers[i]);
                SpecularSamplers.Add(samp1);
            }

            var bindingSet = GraphicsDevice.CreateBindingSet(
                new BindingSetDescription([
                    new(0, BindingResourceType.TextureSrv, tex),
                    new(1, BindingResourceType.TextureSrv, tex1),
                    new(0, BindingResourceType.Sampler, samp),
                    new(1, BindingResourceType.Sampler, samp1)]
                ),
                ResourceFactoryCache.PhongBindingLayout);

            BindingSets[mesh.MaterialIndex] = bindingSet;
            Materials[mesh.MaterialIndex] = new Graphics.Material(ResourceFactoryCache.PhongPipeline, bindingSet, null);
        }
    }

    private static void LoadTextures(Assimp.Material material, TextureType type,
        List<TextureDescription> textures,
        List<byte[]> texturesData,
        List<SamplerDescription> samplers)
    {
        var slots = material.GetMaterialTextures(type);

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];

            using (var stream = File.OpenRead(Path.Combine("resources/objects/backpack", slot.FilePath)))
            {
                ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                textures.Add(TextureDescription.Texture2D((uint)image.Width, (uint)image.Height, mipLevels: 0));
                texturesData.Add(image.Data);
            }

            samplers.Add(SamplerDescription.AnisotropicWrap(16.0f) with
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
            _ => throw new NotSupportedException()
        };

    public static void Dispose()
    {
        foreach (var samp in DiffuseSamplers)
        {
            samp?.Dispose();
        }

        foreach (var samp in SpecularSamplers)
        {
            samp?.Dispose();
        }

        foreach (var tex in DiffuseTextures)
        {
            tex?.Dispose();
        }

        foreach (var tex in SpecularTextures)
        {
            tex?.Dispose();
        }

        foreach (var set in BindingSets)
        {
            set?.Dispose();
        }

        foreach (var mat in Materials)
        {
            mat?.Dispose();
        }
    }
}
