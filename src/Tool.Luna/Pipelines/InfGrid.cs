using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Tool.Assets;
using Tool.Graphics;

namespace Tool.Luna;

public static class InfGrid
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InfGridPerFrame
    {
        public Matrix4x4 ViewProj;
        public Vector3 CamWorldPos;
        public float Pad0;
    };

    public sealed class InfGridPerFrameData : DynamicConstantBuffer<InfGridPerFrame>
    {
        public InfGridPerFrameData(BufferHandle buffer) : base(buffer)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetViewProj(in Matrix4x4 viewProj)
        {
            _data.ViewProj = viewProj;
            _isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCamWorldPos(Vector3 camWorldPos)
        {
            _data.CamWorldPos = camWorldPos;
            _isDirty = true;
        }
    }

    public static AssetHandle<ShaderAsset> VS;
    public static AssetHandle<ShaderAsset> PS;

    public static BindingLayoutHandle PerFrameBL;

    public static GraphicsPipelineHandle Pipeline;

    // Per frame
    public static BufferHandle PerFrameCB;
    public static BindingSetHandle PerFrameBS;

    public static void Load(GraphicsDevice device, AssetScope scope)
    {
        VS = scope.LoadShader("Shaders/InfiniteGrid.hlsl", ShaderDescription.Vertex(debug: true, debugName: "InfiniteGrid.vs"));
        PS = scope.LoadShader("Shaders/InfiniteGrid.hlsl", ShaderDescription.Pixel(debug: true, debugName: "InfiniteGrid.ps"));

        var vs = scope.Get(VS).Handle;
        var ps = scope.Get(PS).Handle;

        PerFrameBL = device.GetOrCreateBindingLayout(new BindingLayoutDescription(
            Visibility: ShaderType.Vertex,
            Bindings: [
                new(0, BindingResourceType.ConstantBuffer)
                ],
            RegisterSpace: 0
            ));

        Pipeline = device.GetOrCreateGraphicsPipeline(new(
            vs,
            ps,
            default,
            new(PerFrameBL),
            Resources.CullNone,
            BlendState: Resources.Opaque,
            PrimitiveTopology.TriangleList));

        // Per frame
        PerFrameCB = device.CreateBuffer(BufferDescription.Constant<InfGridPerFrame>());
        PerFrameBS = device.CreateBindingSet(
            desc: new(
                Bindings: [
                    BindingSetItem.ConstantBuffer(0, PerFrameCB)
                ]),
            layout: PerFrameBL);
    }

    public static void Unload(GraphicsDevice device)
    {
        device.Destroy(PerFrameBS);
        device.Destroy(PerFrameCB);
    }
}
