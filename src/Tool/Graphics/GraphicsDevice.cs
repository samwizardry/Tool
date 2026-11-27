using Tool.Core;

using Vortice.Mathematics;

namespace Tool.Graphics;

public abstract class GraphicsDevice : IDisposable
{
    protected ulong _currentFrameIndex;
    protected virtual ulong FramesInFlight => 2;

    public virtual GraphicsAPI API { get; }

    public virtual SizeI ClientSize { get; protected set; }

    public virtual bool VSync { get; set; }

    public virtual Color4 ClearColor { get; set; }

    #region Window & Display State Notifications

    public abstract void OnResize(int width, int height);

    public abstract void OnWindowMoved(RectI bounds);

    public abstract void OnWindowModeChanged(WindowMode mode);

    public abstract void OnMinimized();

    public abstract void OnRestored();

    #endregion

    #region Frame Lifecycle & Render Target Control

    public abstract bool BeginFrame();

    public abstract bool EndFrame();

    public abstract void WaitIdle();

    #endregion

    #region Resource Creation & Binding

    // Immutable States: cached & owned by GraphicsDevice until shutdown
    public abstract InputLayoutHandle GetOrCreateInputLayout(in VertexLayout layout, ShaderHandle vs);
    public abstract RasterStateHandle GetOrCreateRasterState(in RasterStateDescription desc);
    public abstract SamplerHandle GetOrCreateSampler(in SamplerDescription desc);
    public abstract BlendStateHandle GetOrCreateBlendState(in BlendStateDescription desc);
    public abstract BindingLayoutHandle GetOrCreateBindingLayout(in BindingLayoutDescription desc);
    public abstract GraphicsPipelineHandle GetOrCreateGraphicsPipeline(in GraphicsPipelineDescription desc);

    // Data Resources: owned by caller, destroyed via DeferredDeletionQueue
    public abstract BufferHandle CreateBuffer<T>(ReadOnlySpan<T> data, BufferDescription desc) where T : unmanaged;
    public abstract BufferHandle CreateBuffer(BufferDescription desc);
    public abstract TextureHandle CreateTexture<T>(ReadOnlySpan<T> data, in TextureDescription desc) where T : unmanaged;
    public abstract TextureHandle CreateTexture(in TextureDescription desc);
    public abstract ShaderHandle CreateShader(ReadOnlySpan<byte> source, in ShaderDescription desc);
    public abstract BindingSetHandle CreateBindingSet(BindingSetDescription desc, BindingLayoutHandle layout);

    public abstract BufferDescription GetBufferDescription(BufferHandle buffer);
    public abstract ref readonly TextureDescription GetTextureDescription(TextureHandle texture);

    public abstract void UpdateBuffer<T>(BufferHandle buffer, ReadOnlySpan<T> data, uint byteOffset = 0) where T : unmanaged;
    public abstract void UpdateBuffer<T>(BufferHandle buffer, in T data, uint byteOffset = 0) where T : unmanaged;
    public abstract void UpdateTexture<T>(TextureHandle texture, ReadOnlySpan<T> data, uint mipLevel = 0, uint arraySlice = 0) where T : unmanaged;

    // Never frees immediately while GPU may use it; schedules into DeferredDeletionQueue
    public abstract void Destroy(BufferHandle buffer);
    public abstract void Destroy(TextureHandle texture);
    public abstract void Destroy(ShaderHandle shader);
    public abstract void Destroy(BindingSetHandle bindingSet);

    public abstract void SetGraphicsState(in GraphicsState state);

    #endregion

    #region Drawing Commands (Direct, Instanced, Indirect)

    public abstract void Draw(uint vertexCount, uint startVertexLocation = 0);

    public abstract void DrawIndexed(uint indexCount, uint startIndexLocation = 0, int baseVertexLocation = 0);

    public abstract void DrawInstanced(
        uint vertexCountPerInstance,
        uint instanceCount,
        uint startVertexLocation = 0,
        uint startInstanceLocation = 0);

    public abstract void DrawIndexedInstanced(
        uint indexCountPerInstance,
        uint instanceCount,
        uint startIndexLocation = 0,
        int baseVertexLocation = 0,
        uint startInstanceLocation = 0);

    public abstract void DrawIndirect(BufferHandle buffer, uint alignedByteOffset = 0);

    public abstract void DrawIndexedIndirect(BufferHandle buffer, uint alignedByteOffset = 0);

    #endregion

    #region Compute Commands

    public abstract void Dispatch(uint threadGroupCountX, uint threadGroupCountY = 1, uint threadGroupCountZ = 1);

    public abstract void DispatchIndirect(BufferHandle buffer, uint alignedByteOffset = 0);

    #endregion

    #region Disposing

    protected bool _disposed = false;

    public void Dispose()
    {
        if (_disposed)
            return;

        FreeResources();

        _disposed = true;
    }

    protected abstract void FreeResources();

    #endregion
}
