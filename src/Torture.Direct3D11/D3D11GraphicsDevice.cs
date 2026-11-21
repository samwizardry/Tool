using SharpGen.Runtime;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Debug;
using Vortice.DXGI;
using Vortice.DXGI.Debug;
using Vortice.Mathematics;

using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace Torture.Direct3D11;

/// <inheritdoc />
public sealed class D3D11GraphicsDevice : CoreGraphicsDevice
{
    private const int BufferCount = 2;

    private static readonly FeatureLevel[] FeatureLevels = [
        FeatureLevel.Level_11_1,
        FeatureLevel.Level_11_0];

    private readonly bool _tearingSupported;
    private readonly SwapChainFlags _swapChainFlags;
    private readonly Format _colorFormat;
    private readonly Format _depthStencilFormat;

#if GRAPHICS_DEBUG
    private readonly int _renderThreadId;
    private readonly EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs>? _onFirstChanceException;
    private readonly ID3D11InfoQueue? _infoQueue;
#endif

    // Direct3D 11 interface
    private readonly IDXGIFactory7 _factory;
    private readonly ID3D11Device5 _device;
    private readonly ID3D11DeviceContext4 _imContext;
    private readonly IDXGISwapChain1 _swapChain;
    private ID3D11Texture2D _rtvBackBuffer = null!;
    private ID3D11RenderTargetView _renderTargetView = null!;
    private ID3D11Texture2D _depthStencilBuffer = null!;
    private ID3D11DepthStencilView _depthStencilView = null!;

    // Cached state for SetGraphicsState diffing
    private D3D11GraphicsPipeline? _currentPipeline;
    private CoreBindingSetVector _currentBindings;
    private CoreVertexBufferBinding? _currentVertexBuffer;
    private CoreIndexBufferBinding? _currentIndexBuffer;

    public D3D11GraphicsDevice(
        nint hwnd,
        SizeI clientSize,
        Format colorFormat = Format.B8G8R8A8_UNorm,
        Format depthStencilFormat = Format.D24_UNorm_S8_UInt)
    {
#if GRAPHICS_DEBUG
            Configuration.EnableObjectTracking = true;
#endif

        try
        {
            ClientSize = clientSize;

            /*
             * Native format for Windows (DWM and Direct2D): the Desktop Window Manager internally operates in BGRA8.
             * Always prefer B8G8R8A8_UNorm for the swap chain back buffer.
             */
            _colorFormat = colorFormat;

            /*
             * Depth-Stencil Buffer serves two purposes:
             * Depth (Z-buffer): stores distance from camera to each pixel in range [0.0 .. 1.0] so closer objects occlude farther ones.
             * Stencil: stores an 8-bit integer (0..255) per pixel. Used for masking, outlines, portals, decals.
             *
             * Format                       Depth Precision     Stencil Precision
             * Format.D24_UNorm_S8_UInt     24 bits             8 bits (0..255)
             *      Classic versatile choice when stencil is required for small/medium scenes.
             *
             * Format.D32_Float             32 bits             None (0 bits)
             *      Modern standard for 3D engines and large open spaces when stencil is not needed.
             *      Provides higher depth precision for the same 4 bytes/pixel, preventing distant Z-fighting.
             *
             * Format.D32_Float_S8X24_UInt  32 bits             8 bits (+24 alignment padding bits)
             *      When both 32-bit depth precision and stencil are required (8 bytes per pixel).
             *
             * Format.Unknown               None                None
             *      For pure 2D rendering without depth testing (conserves VRAM).
             */
            _depthStencilFormat = depthStencilFormat;

            _factory = CreateDXGIFactory1<IDXGIFactory7>();

            _tearingSupported = _factory.PresentAllowTearing;

            using (IDXGIAdapter1 adapter = GetHardwareAdapter())
            {
                DeviceCreationFlags deviceCreationFlags = DeviceCreationFlags.BgraSupport;

#if GRAPHICS_DEBUG
                if (SdkLayersAvailable())
                {
                    deviceCreationFlags |= DeviceCreationFlags.Debug;
                }
                else
                {
                    Log.Warning(D3D11LogCategories.D3D11, "Validation requested, but D3D11 SDK layers are not installed.");
                }
#endif

                ID3D11Device? device = null;
                ID3D11DeviceContext? imContext = null;

                if (D3D11CreateDevice(
                    adapter,
                    DriverType.Unknown,
                    deviceCreationFlags,
                    FeatureLevels,
                    out device,
                    out FeatureLevel featureLevel,
                    out imContext).Failure)
                {
                    device?.Dispose();
                    imContext?.Dispose();

                    // If the initialization fails, fall back to the WARP device.
                    // For more information on WARP, see:
                    // http://go.microsoft.com/fwlink/?LinkId=286690
                    D3D11CreateDevice(
                        IntPtr.Zero,
                        DriverType.Warp,
                        deviceCreationFlags,
                        FeatureLevels,
                        out device,
                        out featureLevel,
                        out imContext).CheckError();
                }

                _device = device.QueryInterface<ID3D11Device5>();
                _imContext = imContext.QueryInterface<ID3D11DeviceContext4>();

                device.Dispose();
                imContext.Dispose();

                if (!FeatureLevels.Contains(featureLevel))
                {
                    throw new NotSupportedException("Direct3D Feature Level 11 not supported.");
                }
            }

#if GRAPHICS_DEBUG
            _infoQueue = _device.QueryInterfaceOrNull<ID3D11InfoQueue>();
            if (_infoQueue != null)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    _infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
                    _infoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
                }

                CoreLogSeverity effectiveSeverity = Log.MinimumSeverity > D3D11LogCategories.D3D11.MinimumSeverity
                    ? Log.MinimumSeverity
                    : D3D11LogCategories.D3D11.MinimumSeverity;

                if (!Log.IsEnabled(effectiveSeverity, D3D11LogCategories.D3D11))
                {
                    effectiveSeverity = CoreLogSeverity.None;
                }

                MessageSeverity[] denyList;

                switch (effectiveSeverity)
                {
                    case CoreLogSeverity.Message:
                        denyList = [];
                        break;
                    case CoreLogSeverity.Info:
                        denyList = [MessageSeverity.Message];
                        break;
                    case CoreLogSeverity.Warning:
                        denyList = [MessageSeverity.Message, MessageSeverity.Info];
                        break;
                    case CoreLogSeverity.Error:
                        denyList = [MessageSeverity.Message, MessageSeverity.Info, MessageSeverity.Warning];
                        break;
                    case CoreLogSeverity.Fatal:
                        denyList = [MessageSeverity.Message, MessageSeverity.Info, MessageSeverity.Warning, MessageSeverity.Error];
                        break;
                    case CoreLogSeverity.None:
                        denyList = [MessageSeverity.Message, MessageSeverity.Info, MessageSeverity.Warning, MessageSeverity.Error, MessageSeverity.Corruption];
                        break;
                    default:
                        throw new NotSupportedException();
                }

                _infoQueue.PushStorageFilter(new Vortice.Direct3D11.Debug.InfoQueueFilter
                {
                    DenyList = new Vortice.Direct3D11.Debug.InfoQueueFilterDescription { Severities = denyList }
                });

                _renderThreadId = Environment.CurrentManagedThreadId;
                _onFirstChanceException = (_, args) =>
                {
                    if (args.Exception is SharpGenException && Environment.CurrentManagedThreadId == _renderThreadId)
                    {
                        try
                        {
                            FlushDebugMessages();
                        }
                        catch
                        {
                            // Never throw from a first-chance handler.
                        }
                    }
                };

                AppDomain.CurrentDomain.FirstChanceException += _onFirstChanceException;
            }
#endif

            _swapChainFlags = SwapChainFlags.None;
            if (_tearingSupported)
            {
                _swapChainFlags |= SwapChainFlags.AllowTearing;
            }

            SwapChainDescription1 swapChainDescription = new()
            {
                Width = (uint)clientSize.Width,
                Height = (uint)clientSize.Height,
                Format = _colorFormat,
                Stereo = false,
                SampleDescription = SampleDescription.Default,
                BufferUsage = Usage.RenderTargetOutput,
                BufferCount = BufferCount,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                AlphaMode = AlphaMode.Ignore,
                Flags = _swapChainFlags
            };

            SwapChainFullscreenDescription fullscreenDescription = new()
            {
                RefreshRate = new Rational(0, 0),
                ScanlineOrdering = ModeScanlineOrder.Unspecified,
                Scaling = ModeScaling.Unspecified,
                Windowed = true
            };

            _swapChain = _factory.CreateSwapChainForHwnd(_device, hwnd, swapChainDescription, fullscreenDescription);
            _factory.MakeWindowAssociation(hwnd, WindowAssociationFlags.IgnoreAltEnter)
                .CheckError();

            // Handle color space settings for HDR.
            UpdateColorSpace();

            OnResize(clientSize.Width, clientSize.Height);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public override CoreGraphicsAPI API => CoreGraphicsAPI.D3D11;

    /// <inheritdoc />
    public override SizeI ClientSize { get; protected set; }

    /// <inheritdoc />
    public override Viewport Viewport { get; protected set; }

    /// <inheritdoc />
    public override RectI ScissorRect { get; protected set; }

    /// <inheritdoc />
    public override bool VSync { get; set; }

    /// <inheritdoc />
    public override Color4 ClearColor { get; set; } = Colors.CornflowerBlue;

    #region Window & Display State Notifications

    /// <inheritdoc />
    public override void OnResize(int width, int height)
    {
        ClientSize = new SizeI(width, height);
        Viewport = new Viewport(width, height);
        ScissorRect = new RectI(width, height);

        _imContext.OMSetRenderTargets((ID3D11RenderTargetView)null!);

        // Release the old views, as they hold references to the buffers we
        // will be destroying. Also release the old depth/stencil buffer.
        _renderTargetView?.Dispose();
        _rtvBackBuffer?.Dispose();
        _depthStencilView?.Dispose();
        _depthStencilBuffer?.Dispose();

        // Resize the swap chain and recreate the render target view.
        _swapChain.ResizeBuffers(BufferCount, (uint)width, (uint)height, _colorFormat, _swapChainFlags).CheckError();

        _rtvBackBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        _renderTargetView = _device.CreateRenderTargetView(_rtvBackBuffer);

        // Create the depth/stencil buffer and view.
        _depthStencilBuffer = _device.CreateTexture2D(_depthStencilFormat, (uint)width, (uint)height,
                arraySize: 1, mipLevels: 1, initialData: null,
                bindFlags: BindFlags.DepthStencil, miscFlags: ResourceOptionFlags.None,
                usage: ResourceUsage.Default, cpuAccessFlags: CpuAccessFlags.None);

        _depthStencilView = _device.CreateDepthStencilView(_depthStencilBuffer, new DepthStencilViewDescription(_depthStencilBuffer, DepthStencilViewDimension.Texture2D));

        // Bind the render target view and depth/stencil view to the pipeline.
        _imContext.OMSetRenderTargets(_renderTargetView, _depthStencilView);

        // Set the viewport and scissor rect.
        _imContext.RSSetViewport(Viewport);
        _imContext.RSSetScissorRect(ScissorRect);
    }

    /// <inheritdoc />
    public override void OnWindowMoved(RectI bounds) { }

    /// <inheritdoc />
    public override void OnWindowModeChanged(CoreWindowMode mode) { }

    /// <inheritdoc />
    public override void OnMinimized() { }

    /// <inheritdoc />
    public override void OnRestored() { }

    #endregion

    #region Frame Lifecycle & Render Target Control

    /// <inheritdoc />
    public override void Clear()
    {
        _imContext.ClearRenderTargetView(_renderTargetView, ClearColor);
        _imContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
    }

    /// <inheritdoc />
    public override bool BeginFrame()
    {
        _currentPipeline = null;
        _currentBindings = default;
        _currentVertexBuffer = null;
        _currentIndexBuffer = null;

        _imContext.ClearRenderTargetView(_renderTargetView, ClearColor);
        _imContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

        _imContext.OMSetRenderTargets(_renderTargetView, _depthStencilView);
        _imContext.RSSetViewport(Viewport);
        _imContext.RSSetScissorRect(ScissorRect);

        return true;
    }

    /// <inheritdoc />
    public override bool EndFrame()
    {
        PresentFlags presentFlags = PresentFlags.None;
        uint syncInterval = 0u;

        if (VSync)
        {
            syncInterval = 1u;
        }
        else if (_tearingSupported)
        {
            presentFlags |= PresentFlags.AllowTearing;
        }

#if GRAPHICS_DEBUG
        var result = _swapChain.Present(syncInterval, presentFlags);
        FlushDebugMessages();
        result.CheckError();
#else
        _swapChain.Present(syncInterval, presentFlags);
#endif

        return true;
    }

    /// <inheritdoc />
    public override void WaitIdle()
    {
        _imContext.Flush();
    }

    #endregion

    #region Resource Creation & Binding

    /// <inheritdoc />
    public override CoreRasterState CreateRasterState(in CoreRasterStateDesc desc)
    {
        return new D3D11RasterState(_device, in desc);
    }

    /// <inheritdoc />
    public override CoreBuffer CreateBuffer<T>(ReadOnlySpan<T> data, in CoreBufferDesc desc)
    {
        return D3D11Buffer.Create(_device, data, in desc);
    }

    /// <inheritdoc />
    public override CoreBuffer CreateBuffer(in CoreBufferDesc desc)
    {
        return D3D11Buffer.Create(_device, in desc);
    }

    /// <inheritdoc />
    public override CoreShader CreateShader(ReadOnlySpan<byte> source, in CoreShaderDesc desc)
    {
        return new D3D11Shader(_device, source, in desc);
    }

    /// <inheritdoc />
    public override CoreInputLayout CreateInputLayout(in CoreVertexLayout layout, CoreShader shader)
    {
        return new D3D11InputLayout(_device, in layout, (D3D11Shader)shader);
    }

    /// <inheritdoc />
    public override CoreTexture CreateTexture<T>(ReadOnlySpan<T> data, in CoreTextureDesc desc)
    {
        return D3D11Texture.Create(_device, _imContext, data, in desc);
    }

    /// <inheritdoc />
    public override CoreTexture CreateTexture(in CoreTextureDesc desc)
    {
        return D3D11Texture.Create(_device, in desc);
    }

    /// <inheritdoc />
    public override CoreSampler CreateSampler(in CoreSamplerDesc desc)
    {
        return new D3D11Sampler(_device, in desc);
    }

    /// <inheritdoc />
    public override CoreGraphicsPipeline CreateGraphicsPipeline(in CoreGraphicsPipelineDesc desc)
    {
        return new D3D11GraphicsPipeline(in desc);
    }

    /// <inheritdoc />
    public override CoreBindingLayout CreateBindingLayout(in CoreBindingLayoutDesc desc)
    {
        return new D3D11BindingLayout(in desc);
    }

    /// <inheritdoc />
    public override CoreBindingSet CreateBindingSet(in CoreBindingSetDesc desc, CoreBindingLayout layout)
    {
        return new D3D11BindingSet(desc, (D3D11BindingLayout)layout);
    }

    /// <inheritdoc />
    public override unsafe void UpdateBuffer<T>(CoreBuffer buffer, ReadOnlySpan<T> data, uint byteOffset = 0)
    {
#if GRAPHICS_DEBUG
        if (data.IsEmpty)
        {
            Log.Warning(D3D11LogCategories.D3D11, "Empty data received.");
            return;
        }
#endif

        var d3d11Buffer = (D3D11Buffer)buffer;
        uint dataByteSize = (uint)(sizeof(T) * data.Length);

#if GRAPHICS_DEBUG
        if (byteOffset + dataByteSize > d3d11Buffer.ByteSize)
        {
            Log.Error(D3D11LogCategories.D3D11, "Data size exceeds buffer capacity.");
            return;
        }
#endif

        fixed (T* srcPtr = data)
        {
            MapMode mode = byteOffset == 0 ? MapMode.WriteDiscard : MapMode.WriteNoOverwrite;
            var map = _imContext.Map(d3d11Buffer.Native, 0, mode, Vortice.Direct3D11.MapFlags.None);

            byte* dstPtr = (byte*)map.DataPointer + byteOffset;
            System.Buffer.MemoryCopy(srcPtr, dstPtr, d3d11Buffer.ByteSize - byteOffset, dataByteSize);

            _imContext.Unmap(d3d11Buffer.Native, 0);
        }
    }

    /// <inheritdoc />
    public override unsafe void UpdateBuffer<T>(CoreBuffer buffer, ref T data, uint byteOffset = 0)
    {
        var d3d11Buffer = (D3D11Buffer)buffer;

#if GRAPHICS_DEBUG
        if (byteOffset + (uint)sizeof(T) > d3d11Buffer.ByteSize)
        {
            Log.Error(D3D11LogCategories.D3D11, "Data size exceeds buffer capacity.");
            return;
        }
#endif

        MapMode mode = byteOffset == 0 ? MapMode.WriteDiscard : MapMode.WriteNoOverwrite;
        var map = _imContext.Map(d3d11Buffer.Native, 0, mode, Vortice.Direct3D11.MapFlags.None);

        *(T*)((byte*)map.DataPointer + byteOffset) = data;

        _imContext.Unmap(d3d11Buffer.Native, 0);
    }

    /// <inheritdoc />
    public override unsafe void UpdateTexture<T>(CoreTexture texture, ReadOnlySpan<T> data, uint mipLevel = 0, uint arraySlice = 0)
    {
        var d3d11Texture = (D3D11Texture)texture;

#if GRAPHICS_DEBUG
        if (mipLevel >= d3d11Texture.MipLevels)
        {
            Log.Error(D3D11LogCategories.D3D11, "Mip level exceeds texture mip count.");
            return;
        }

        if (arraySlice >= d3d11Texture.ArraySize)
        {
            Log.Error(D3D11LogCategories.D3D11, "Array slice exceeds texture array size.");
            return;
        }
#endif

        uint mipWidth = System.Math.Max(1u, d3d11Texture.Width >> (int)mipLevel);
        uint mipHeight = System.Math.Max(1u, d3d11Texture.Height >> (int)mipLevel);
        uint mipDepth = System.Math.Max(1u, d3d11Texture.Depth >> (int)mipLevel);
        uint subresource = mipLevel + (arraySlice * d3d11Texture.MipLevels);
        CoreFormatInfo.GetSurfaceInfo(d3d11Texture.Format, mipWidth, mipHeight, out uint rowPitch, out uint slicePitch);

#if GRAPHICS_DEBUG
        uint expectedBytes = slicePitch * mipDepth;
        uint actualBytes = (uint)(sizeof(T) * data.Length);

        if (actualBytes < expectedBytes)
        {
            Log.Error(D3D11LogCategories.D3D11, $"Provided data size ({actualBytes} bytes) is smaller than the target mip level size ({expectedBytes} bytes).");
            return;
        }
#endif

        fixed (T* srcPtr = data)
        {
            _imContext.UpdateSubresource(d3d11Texture.Native, subresource, null, (nint)srcPtr, rowPitch, slicePitch);
        }
    }

    /// <inheritdoc />
    public override void SetRasterState(CoreRasterState state)
    {
        _imContext.RSSetState(((D3D11RasterState)state).Native);
    }

    /// <inheritdoc />
    public override void SetPrimitiveTopology(CorePrimitiveTopology primitiveTopology)
    {
        _imContext.IASetPrimitiveTopology(primitiveTopology.ToD3D11PrimitiveTopology());
    }

    /// <inheritdoc />
    public override void SetVertexBuffer(CoreBuffer buffer, uint slot = 0, uint byteOffset = 0)
    {
        var d3d11Buffer = (D3D11Buffer)buffer;
        _imContext.IASetVertexBuffer(slot, d3d11Buffer.Native, d3d11Buffer.ByteStride, byteOffset);
    }

    /// <inheritdoc />
    public override void SetIndexBuffer(CoreBuffer buffer, uint byteOffset = 0)
    {
        var d3d11Buffer = (D3D11Buffer)buffer;
        _imContext.IASetIndexBuffer(d3d11Buffer.Native, ((CoreIndexFormat)d3d11Buffer.ByteStride).ToDXGIFormat(), byteOffset);
    }

    /// <inheritdoc />
    public override void SetConstantBuffer(CoreBuffer buffer, CoreShaderType stage, uint slot)
    {
        var d3d11Buffer = (D3D11Buffer)buffer;

        if ((stage & CoreShaderType.Vertex) == CoreShaderType.Vertex)
        {
            _imContext.VSSetConstantBuffer(slot, d3d11Buffer.Native);
        }
        if ((stage & CoreShaderType.Hull) == CoreShaderType.Hull)
        {
            _imContext.HSSetConstantBuffer(slot, d3d11Buffer.Native);
        }
        if ((stage & CoreShaderType.Domain) == CoreShaderType.Domain)
        {
            _imContext.DSSetConstantBuffer(slot, d3d11Buffer.Native);
        }
        if ((stage & CoreShaderType.Geometry) == CoreShaderType.Geometry)
        {
            _imContext.GSSetConstantBuffer(slot, d3d11Buffer.Native);
        }
        if ((stage & CoreShaderType.Pixel) == CoreShaderType.Pixel)
        {
            _imContext.PSSetConstantBuffer(slot, d3d11Buffer.Native);
        }
        if ((stage & CoreShaderType.Compute) == CoreShaderType.Compute)
        {
            _imContext.CSSetConstantBuffer(slot, d3d11Buffer.Native);
        }
    }

    /// <inheritdoc />
    public override void SetVertexShader(CoreShader shader)
    {
        _imContext.VSSetShader((ID3D11VertexShader)((D3D11Shader)shader).Native);
    }

    /// <inheritdoc />
    public override void SetPixelShader(CoreShader shader)
    {
        _imContext.PSSetShader((ID3D11PixelShader)((D3D11Shader)shader).Native);
    }

    /// <inheritdoc />
    public override void SetInputLayout(CoreInputLayout inputLayout)
    {
        _imContext.IASetInputLayout(((D3D11InputLayout)inputLayout).Native);
    }

    /// <inheritdoc />
    public override void SetTexture(CoreTexture texture, CoreShaderType stage, uint slot)
    {
        var d3d11Texture = (D3D11Texture)texture;

        if ((stage & CoreShaderType.Vertex) == CoreShaderType.Vertex)
        {
            _imContext.VSSetShaderResource(slot, d3d11Texture.Srv);
        }
        if ((stage & CoreShaderType.Hull) == CoreShaderType.Hull)
        {
            _imContext.HSSetShaderResource(slot, d3d11Texture.Srv);
        }
        if ((stage & CoreShaderType.Domain) == CoreShaderType.Domain)
        {
            _imContext.DSSetShaderResource(slot, d3d11Texture.Srv);
        }
        if ((stage & CoreShaderType.Geometry) == CoreShaderType.Geometry)
        {
            _imContext.GSSetShaderResource(slot, d3d11Texture.Srv);
        }
        if ((stage & CoreShaderType.Pixel) == CoreShaderType.Pixel)
        {
            _imContext.PSSetShaderResource(slot, d3d11Texture.Srv!);
        }
        if ((stage & CoreShaderType.Compute) == CoreShaderType.Compute)
        {
            _imContext.CSSetShaderResource(slot, d3d11Texture.Srv);
        }
    }

    /// <inheritdoc />
    public override void SetSampler(CoreSampler sampler, CoreShaderType stage, uint slot)
    {
        var d3d11Sampler = (D3D11Sampler)sampler;

        if ((stage & CoreShaderType.Vertex) == CoreShaderType.Vertex)
        {
            _imContext.VSSetSampler(slot, d3d11Sampler.Native);
        }
        if ((stage & CoreShaderType.Hull) == CoreShaderType.Hull)
        {
            _imContext.HSSetSampler(slot, d3d11Sampler.Native);
        }
        if ((stage & CoreShaderType.Domain) == CoreShaderType.Domain)
        {
            _imContext.DSSetSampler(slot, d3d11Sampler.Native);
        }
        if ((stage & CoreShaderType.Geometry) == CoreShaderType.Geometry)
        {
            _imContext.GSSetSampler(slot, d3d11Sampler.Native);
        }
        if ((stage & CoreShaderType.Pixel) == CoreShaderType.Pixel)
        {
            _imContext.PSSetSampler(slot, d3d11Sampler.Native);
        }
        if ((stage & CoreShaderType.Compute) == CoreShaderType.Compute)
        {
            _imContext.CSSetSampler(slot, d3d11Sampler.Native);
        }
    }

    /// <inheritdoc />
    public override void SetGraphicsState(in CoreGraphicsState state)
    {
        var pipeline = (D3D11GraphicsPipeline)state.Pipeline;
        if (!ReferenceEquals(_currentPipeline, pipeline))
        {
            pipeline.Apply(_imContext);
            _currentPipeline = pipeline;
        }

        for (int i = 0; i < CoreBindingSetVector.MaxBindingSets; ++i)
        {
            var set = state.Bindings[i];

            if (!ReferenceEquals(_currentBindings[i], set))
            {
                // care: the previous binding set may be active
                if (set != null)
                {
                    ((D3D11BindingSet)set).Apply(_imContext);
                    _currentBindings[i] = set;
                }
            }
        }

        if (_currentVertexBuffer != state.VertexBuffer)
        {
            if (state.VertexBuffer is { } vb)
            {
                SetVertexBuffer(vb.Buffer, vb.Slot, vb.ByteOffset);
            }

            _currentVertexBuffer = state.VertexBuffer;
        }

        if (_currentIndexBuffer != state.IndexBuffer)
        {
            if (state.IndexBuffer is { } ib)
            {
                SetIndexBuffer(ib.Buffer, ib.ByteOffset);
            }

            _currentIndexBuffer = state.IndexBuffer;
        }
    }

#endregion

    #region Drawing Commands (Direct, Instanced, Indirect)

    /// <inheritdoc />
    public override void Draw(uint vertexCount, uint startVertexLocation = 0)
    {
        _imContext.Draw(vertexCount, startVertexLocation);
    }

    /// <inheritdoc />
    public override void DrawIndexed(uint indexCount, uint startIndexLocation = 0, int baseVertexLocation = 0)
    {
        _imContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
    }

    /// <inheritdoc />
    public override void DrawInstanced(
        uint vertexCountPerInstance,
        uint instanceCount,
        uint startVertexLocation = 0,
        uint startInstanceLocation = 0)
    {
        _imContext.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
    }

    /// <inheritdoc />
    public override void DrawIndexedInstanced(
        uint indexCountPerInstance,
        uint instanceCount,
        uint startIndexLocation = 0,
        int baseVertexLocation = 0,
        uint startInstanceLocation = 0)
    {
        _imContext.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
    }

    /// <inheritdoc />
    public override void DrawIndirect(CoreBuffer buffer, uint alignedByteOffset = 0)
    {
        _imContext.DrawInstancedIndirect(((D3D11Buffer)buffer).Native, alignedByteOffset);
    }

    /// <inheritdoc />
    public override void DrawIndexedIndirect(CoreBuffer buffer, uint alignedByteOffset = 0)
    {
        _imContext.DrawIndexedInstancedIndirect(((D3D11Buffer)buffer).Native, alignedByteOffset);
    }

    #endregion

    #region Compute Commands

    /// <inheritdoc />
    public override void Dispatch(uint threadGroupCountX, uint threadGroupCountY = 1, uint threadGroupCountZ = 1)
    {
        _imContext.Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    /// <inheritdoc />
    public override void DispatchIndirect(CoreBuffer buffer, uint alignedByteOffset = 0)
    {
        _imContext.DispatchIndirect(((D3D11Buffer)buffer).Native, alignedByteOffset);
    }

    #endregion

    #region Disposing

    protected override void FreeResources()
    {
        base.FreeResources();

#if GRAPHICS_DEBUG
        if (_onFirstChanceException != null)
        {
            AppDomain.CurrentDomain.FirstChanceException -= _onFirstChanceException;
        }
#endif

        _depthStencilView?.Dispose();
        _depthStencilBuffer?.Dispose();
        _renderTargetView?.Dispose();
        _rtvBackBuffer?.Dispose();

        if (_imContext != null && _imContext.NativePointer != IntPtr.Zero)
        {
            _imContext.ClearState();
            _imContext.Flush();
            _imContext.Dispose();
        }

        _swapChain?.Dispose();

#if GRAPHICS_DEBUG
        if (_infoQueue != null && _device != null)
        {
            if (_device.QueryInterfaceOrNull<ID3D11Debug>() is ID3D11Debug d3d11Debug)
            {
                d3d11Debug.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Detail | ReportLiveDeviceObjectFlags.IgnoreInternal);
                FlushDebugMessages();
                d3d11Debug.Dispose();
            }
        }
        _infoQueue?.Dispose();
#endif

        _device?.Dispose();
        _factory?.Dispose();

#if GRAPHICS_DEBUG
        ReportDxgiLiveObjects();
        ReportSharpGenLeaks();
#endif
    }

    #endregion

    #region Private

    private IDXGIAdapter1 GetHardwareAdapter()
    {
        for (uint adapterIndex = 0;
                _factory.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, out IDXGIAdapter1? adapter).Success;
                adapterIndex++)
        {
            if (adapter == null)
            {
                continue;
            }

            if ((adapter.Description1.Flags & AdapterFlags.Software) != AdapterFlags.None)
            {
                adapter.Dispose();
                continue;
            }

            return adapter;
        }

        for (uint adapterIndex = 0;
            _factory.EnumAdapters1(adapterIndex, out IDXGIAdapter1? adapter).Success;
            adapterIndex++)
        {
            if ((adapter.Description1.Flags & AdapterFlags.Software) != AdapterFlags.None)
            {
                adapter.Dispose();
                continue;
            }

            return adapter;
        }

        throw new InvalidOperationException("Cannot detect Direct3D11 adapter.");
    }

    private void UpdateColorSpace()
    {
    }

#if GRAPHICS_DEBUG
    private static CoreLogSeverity ToLogSeverity(MessageSeverity severity) => severity switch
    {
        MessageSeverity.Corruption => CoreLogSeverity.Fatal,
        MessageSeverity.Error => CoreLogSeverity.Error,
        MessageSeverity.Warning => CoreLogSeverity.Warning,
        MessageSeverity.Info => CoreLogSeverity.Info,
        _ => CoreLogSeverity.Message
    };

    private static CoreLogSeverity ToLogSeverity(InfoQueueMessageSeverity severity) => severity switch
    {
        InfoQueueMessageSeverity.Corruption => CoreLogSeverity.Fatal,
        InfoQueueMessageSeverity.Error => CoreLogSeverity.Error,
        InfoQueueMessageSeverity.Warning => CoreLogSeverity.Warning,
        InfoQueueMessageSeverity.Info => CoreLogSeverity.Info,
        _ => CoreLogSeverity.Message
    };

    private void FlushDebugMessages()
    {
        if (_infoQueue == null || _infoQueue.NativePointer == nint.Zero)
            return;

        ulong count = _infoQueue.NumStoredMessages;
        for (ulong i = 0; i < count; i++)
        {
            Message msg = _infoQueue.GetMessage(i);
            Log.Write(ToLogSeverity(msg.Severity), D3D11LogCategories.D3D11, $"{msg.Category}: {msg.Description}", (int)msg.Id);
        }

        _infoQueue.ClearStoredMessages();
    }

    private static void ReportDxgiLiveObjects()
    {
        if (DXGIGetDebugInterface1(out IDXGIDebug1? dxgiDebug).Failure || dxgiDebug == null)
            return;

        if (DXGIGetDebugInterface1(out IDXGIInfoQueue? dxgiInfoQueue).Success && dxgiInfoQueue != null)
        {
            dxgiInfoQueue.ClearStoredMessages(DebugAll);
            dxgiDebug.ReportLiveObjects(DebugAll, ReportLiveObjectFlags.Detail | ReportLiveObjectFlags.IgnoreInternal);

            ulong count = dxgiInfoQueue.GetNumStoredMessages(DebugAll);
            for (ulong i = 0; i < count; i++)
            {
                InfoQueueMessage msg = dxgiInfoQueue.GetMessage(DebugAll, i);
                Log.Write(ToLogSeverity(msg.Severity), D3D11LogCategories.Dxgi, $"{msg.Category}: {msg.Description}", id: msg.Id);
            }

            dxgiInfoQueue.Dispose();
        }

        dxgiDebug.Dispose();
    }

    private static void ReportSharpGenLeaks()
    {
        var leaks = SharpGen.Runtime.Diagnostics.ObjectTracker.FindActiveObjects();
        foreach (var leak in leaks)
        {
            if (leak.TryGetTarget(out var target))
            {
                Log.Warning(D3D11LogCategories.SharpGen, $"(0x{target.NativePointer:X16}): Live instance of {target.GetType().Name} was not disposed.");
            }
            else
            {
                Log.Warning(D3D11LogCategories.SharpGen, $"Active tracking reference with an already collected target.");
            }
        }
    }
#endif

#endregion
}
