using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Tool.Assets;
using Tool.Graphics;

namespace Tool.Luna;

public static class Phong
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DirectionalLight
    {
        public Vector3 Direction;
        public float Pad0;
        public Vector3 Ambient;
        public float Pad1;
        public Vector3 Diffuse;
        public float Pad2;
        public Vector3 Specular;
        public float Pad3;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PointLight
    {
        public Vector3 Position;
        public float Constant;
        public Vector3 Diffuse;
        public float Linear;
        public Vector3 Specular;
        public float Quadratic;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SpotLight
    {
        public Vector3 Position;
        public float CutOff;
        public Vector3 Direction;
        public float OuterCutOff;
        public Vector3 Diffuse;
        public float Linear;
        public Vector3 Specular;
        public float Quadratic;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Light
    {
        public DirectionalLight DirLight;
        public PointLight PointLight;
        public SpotLight SpotLight;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MaterialData
    {
        public float Shininess;
        public Vector3 Pad0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PerFrameData
    {
        public Matrix4x4 ViewProj;
        public Light Light;
        public Vector3 ViewPos;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PerObjData
    {
        public Matrix4x4 ModelMatrix;
        public Matrix4x4 NormalMatrix;
    }

    public sealed class PerFrameDCB : DynamicConstantBuffer<PerFrameData>
    {
        public PerFrameDCB(BufferHandle buffer)
            : base(buffer)
        {
            _data = default!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetViewProj(in Matrix4x4 viewProj)
        {
            _data.ViewProj = viewProj;
            _isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetViewPos(Vector3 viewPos)
        {
            _data.ViewPos = viewPos;
            _isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDirLight(in DirectionalLight light)
        {
            _data.Light.DirLight = light;
            _isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPointLight(in PointLight light)
        {
            _data.Light.PointLight = light;
            _isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSpotLight(in SpotLight light)
        {
            _data.Light.SpotLight = light;
            _isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSpotLightPosition(Vector3 position)
        {
            _data.Light.SpotLight.Position = position;
            _isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSpotLightDirection(Vector3 direction)
        {
            _data.Light.SpotLight.Direction = direction;
            _isDirty = true;
        }
    }

    public sealed class PerObjDCB : DynamicConstantBuffer<PerObjData>
    {
        public PerObjDCB(BufferHandle buffer)
            : base(buffer)
        {
            _data = default!;
        }

        public ref readonly Matrix4x4 ModelMatrix => ref _data.ModelMatrix;
        public ref readonly Matrix4x4 NormalMatrix => ref _data.NormalMatrix;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetModelMatrix(in Matrix4x4 matrix)
        {
            _data.ModelMatrix = matrix;
            _isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNormalMatrix(in Matrix4x4 matrix)
        {
            _data.NormalMatrix = matrix;
            _isDirty = true;
        }
    }

    public static AssetHandle<ShaderAsset> VS;
    public static AssetHandle<ShaderAsset> PS;

    public static InputLayoutHandle InputLayout;

    public static BindingLayoutHandle PerFrameBL;
    public static BindingLayoutHandle MatBL;
    public static BindingLayoutHandle PerObjBL;

    public static GraphicsPipelineHandle Pipeline;

    // Per frame data
    public static BufferHandle PerFrameCB;
    public static BindingSetHandle PerFrameBS;
    public static PerFrameDCB PerFrame = default!;

    // Textures
    public static AssetHandle<TextureAsset> Container;
    public static AssetHandle<TextureAsset> ContainerSpecular;

    // Materials
    public static AssetHandle<MaterialAsset> CommonMaterial;
    public static AssetHandle<MaterialAsset> BoxMaterial;
    public static MaterialData CommonMatData;
    public static MaterialData BoxMatData;

    public static void Load(GraphicsDevice device, AssetScope scope)
    {
        VS = scope.LoadShader("Shaders/Phong.hlsl", ShaderDescription.Vertex(debug: true, debugName: "phong.vs"));
        PS = scope.LoadShader("Shaders/Phong.hlsl", ShaderDescription.Pixel(debug: true, debugName: "phong.ps"));

        var vs = scope.Get(VS).Handle;
        var ps = scope.Get(PS).Handle;

        InputLayout = device.GetOrCreateInputLayout(VertexPositionNormalTexture.Layout, vs);

        PerFrameBL = device.GetOrCreateBindingLayout(new BindingLayoutDescription(
            visibility: ShaderType.Vertex | ShaderType.Pixel,
            bindings: [
                new(0, BindingResourceType.ConstantBuffer),
            ],
            registerSpace: 0));

        MatBL = device.GetOrCreateBindingLayout(new BindingLayoutDescription(
            visibility: ShaderType.Vertex | ShaderType.Pixel,
            bindings: [
                new(0, BindingResourceType.TextureSrv),
                new(1, BindingResourceType.TextureSrv),
                new(0, BindingResourceType.Sampler)],
            registerSpace: 0));

        PerObjBL = device.GetOrCreateBindingLayout(new BindingLayoutDescription(
            visibility: ShaderType.Vertex | ShaderType.Pixel,
            bindings: [
                new(1, BindingResourceType.ConstantBuffer)
            ],
            registerSpace: 0));

        Pipeline = device.GetOrCreateGraphicsPipeline(new GraphicsPipelineDescription(
            vertexShader: vs,
            pixelShader: ps,
            inputLayout: InputLayout,
            bindingLayouts: new BindingLayoutArray(PerFrameBL, default, MatBL, PerObjBL),
            rasterState: Resources.Solid,
            blendState: Resources.Transparent,
            topology: PrimitiveTopology.TriangleList));

        // Textures
        Container = scope.LoadTexture("resources/textures/container2.png");
        ContainerSpecular = scope.LoadTexture("resources/textures/container2_specular.png");

        // Per frame
        PerFrameCB = device.CreateBuffer(BufferDescription.Constant<PerFrameData>());
        PerFrameBS = device.CreateBindingSet(
            desc: new(
                bindings: [
                    BindingSetItem.ConstantBuffer(0, PerFrameCB)
                ]),
            layout: PerFrameBL);
        PerFrame = new PerFrameDCB(PerFrameCB);

        // Materials
        CommonMaterial = scope.CreateMaterial(new MaterialDescription(
            pipeline: Pipeline,
            bindingLayout: MatBL,
            constantBufferDesc: BufferDescription.Constant<MaterialData>(),
            constantBufferSlot: 2,
            textures: [
                new(0, Resources.WhitePixel),
                new(1, Resources.WhitePixel)],
            samplers: [
                new(0, Resources.Anisotropic16)]
        ));

        BoxMaterial = scope.CreateMaterial(new MaterialDescription(
            pipeline: Pipeline,
            bindingLayout: MatBL,
            constantBufferDesc: BufferDescription.Constant<MaterialData>(),
            constantBufferSlot: 2,
            textures: [
                new(0, Container),
                new(1, ContainerSpecular)],
            samplers: [
                new(0, Resources.Anisotropic16)]
        ));

        // Init variables
        CommonMatData.Shininess = 128.0f;
        BoxMatData.Shininess = 16.0f;
        scope.Manager.UpdateMaterialConstants(CommonMaterial, in CommonMatData);
        scope.Manager.UpdateMaterialConstants(BoxMaterial, in BoxMatData);

        PerFrame.SetDirLight(new DirectionalLight
        {
            Direction = Vector3.Normalize(new Vector3(-0.2f, -1.0f, -0.3f)),
            Ambient = new Vector3(0.1f, 0.1f, 0.1f),
            Diffuse = new Vector3(0.4f, 0.4f, 0.4f),
            Specular = new Vector3(0.5f, 0.5f, 0.5f)
        });

        PerFrame.SetPointLight(new PointLight()
        {
            Position = new Vector3(1.2f, 1.0f, 2.0f),
            Diffuse = new Vector3(0.6f, 0.6f, 0.6f),
            Specular = new Vector3(1.0f, 1.0f, 1.0f),
            Constant = 1.0f,
            Linear = 0.22f,
            Quadratic = 0.20f
        });

        PerFrame.SetSpotLight(new SpotLight()
        {
            Diffuse = new Vector3(0.6f, 0.6f, 0.6f),
            Specular = new Vector3(1.0f, 1.0f, 1.0f),
            CutOff = MathF.Cos(float.DegreesToRadians(12.5f)),
            OuterCutOff = MathF.Cos(float.DegreesToRadians(17.5f)),
            Linear = 0.09f,
            Quadratic = 0.032f,
        });
    }

    public static void Unload(GraphicsDevice device)
    {
        device.Destroy(PerFrameBS);
        device.Destroy(PerFrameCB);
    }
}
