using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Tool.Collections;
using Tool.Core;
using Tool.Logging;

using Vortice.Direct3D;
using Vortice.Direct3D11;
#if GRAPHICS_DEBUG
using Vortice.Direct3D11.Debug;
#endif
using Vortice.DXGI;
#if GRAPHICS_DEBUG
using Vortice.DXGI.Debug;
#endif
using Vortice.Mathematics;

using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace Tool.Direct3D11;

public sealed class D3D11GraphicsDevice : Graphics.GraphicsDevice
{
    private const int SwapChainBufferCount = 2;

    private static readonly FeatureLevel[] FeatureLevels = [
        FeatureLevel.Level_11_1,
        FeatureLevel.Level_11_0];

    private readonly bool _tearingSupported;
    private readonly SwapChainFlags _swapChainFlags;
    private readonly Format _colorFormat;
    private readonly Format _depthStencilFormat;

    // Immutable States: cached & owned by GraphicsDevice until shutdown
    private readonly SlotMap<D3D11InputLayout> _inputLayouts = new();
    private readonly Dictionary<Graphics.VertexLayout, Graphics.InputLayoutHandle> _inputLayoutCache = [];
    private readonly SlotMap<D3D11RasterState> _rasterStates = new();
    private readonly Dictionary<Graphics.RasterStateDescription, Graphics.RasterStateHandle> _rasterStateCache = [];
    private readonly SlotMap<D3D11Sampler> _samplers = new();
    private readonly Dictionary<Graphics.SamplerDescription, Graphics.SamplerHandle> _samplerCache = [];
    private readonly SlotMap<D3D11BlendState> _blendStates = new();
    private readonly Dictionary<Graphics.BlendStateDescription, Graphics.BlendStateHandle> _blendStateCache = [];
    private readonly SlotMap<D3D11BindingLayout> _bindingLayouts = new();
    private readonly Dictionary<Graphics.BindingLayoutDescription, Graphics.BindingLayoutHandle> _bindingLayoutCache = [];
    private readonly SlotMap<D3D11GraphicsPipeline> _pipelines = new();
    private readonly Dictionary<Graphics.GraphicsPipelineDescription, Graphics.GraphicsPipelineHandle> _pipelineCache = [];

    // Data Resources: owned by caller, destroyed via DeferredDeletionQueue
    private readonly SlotMap<D3D11Buffer> _buffers = new();
    private readonly SlotMap<D3D11Texture> _textures = new();
    private readonly SlotMap<D3D11Shader> _shaders = new();
    private readonly SlotMap<D3D11BindingSet> _bindingSets = new();

    // Deferred deletion queues (zero boxing, Vulkan-compatible lifetime model)
    private readonly Queue<(ulong Frame, D3D11Buffer Resource)> _deferredBuffers = new();
    private readonly Queue<(ulong Frame, D3D11Texture Resource)> _deferredTextures = new();
    private readonly Queue<(ulong Frame, D3D11Shader Resource)> _deferredShaders = new();

#if GRAPHICS_DEBUG
    private readonly int _renderThreadId;
    private readonly EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs>? _onFirstChanceException;
    private readonly ID3D11InfoQueue? _infoQueue;
#endif

    // Direct3D 11 interfaces
    private readonly IDXGIFactory7 _factory;
    private readonly ID3D11Device5 _device;
    private readonly ID3D11DeviceContext4 _imContext;
    private readonly IDXGISwapChain1 _swapChain;
    private ID3D11Texture2D _rtvBackBuffer = null!;
    private ID3D11RenderTargetView _renderTargetView = null!;
    private ID3D11Texture2D _depthStencilBuffer = null!;
    private ID3D11DepthStencilView _depthStencilView = null!;

    // Cached state for SetGraphicsState diffing
    private Graphics.GraphicsPipelineHandle _currentPipeline = default;
    private Graphics.ViewportState _currentViewport = default;
    private Color4 _currentBlendConstantColor = default;
    private Graphics.BindingSetArray _currentBindings = default;
    private Graphics.VertexBufferBinding _currentVertexBuffer = default;
    private Graphics.IndexBufferBinding _currentIndexBuffer = default;

    public D3D11GraphicsDevice(
        nint hwnd,
        SizeI clientSize,
        Format colorFormat = Format.B8G8R8A8_UNorm,
        Format depthStencilFormat = Format.D24_UNorm_S8_UInt)
    {
#if GRAPHICS_DEBUG
        SharpGen.Runtime.Configuration.EnableObjectTracking = true;
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

                if (D3D11CreateDevice(
                    adapter,
                    DriverType.Unknown,
                    deviceCreationFlags,
                    FeatureLevels,
                    out ID3D11Device? device,
                    out FeatureLevel featureLevel,
                    out ID3D11DeviceContext? imContext).Failure)
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

                LogSeverity effectiveSeverity = Log.MinimumSeverity > D3D11LogCategories.D3D11.MinimumSeverity
                    ? Log.MinimumSeverity
                    : D3D11LogCategories.D3D11.MinimumSeverity;

                if (!Log.IsEnabled(effectiveSeverity, D3D11LogCategories.D3D11))
                {
                    effectiveSeverity = LogSeverity.None;
                }

                MessageSeverity[] denyList = effectiveSeverity switch
                {
                    LogSeverity.Message => [],
                    LogSeverity.Info => [MessageSeverity.Message],
                    LogSeverity.Warning => [MessageSeverity.Message, MessageSeverity.Info],
                    LogSeverity.Error => [MessageSeverity.Message, MessageSeverity.Info, MessageSeverity.Warning],
                    LogSeverity.Fatal => [MessageSeverity.Message, MessageSeverity.Info, MessageSeverity.Warning, MessageSeverity.Error],
                    LogSeverity.None => [MessageSeverity.Message, MessageSeverity.Info, MessageSeverity.Warning, MessageSeverity.Error, MessageSeverity.Corruption],
                    _ => throw new NotSupportedException(),
                };

                _infoQueue.PushStorageFilter(new Vortice.Direct3D11.Debug.InfoQueueFilter
                {
                    DenyList = new Vortice.Direct3D11.Debug.InfoQueueFilterDescription { Severities = denyList }
                });

                _renderThreadId = Environment.CurrentManagedThreadId;
                _onFirstChanceException = (_, args) =>
                {
                    if (args.Exception is SharpGen.Runtime.SharpGenException && Environment.CurrentManagedThreadId == _renderThreadId)
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
                BufferCount = SwapChainBufferCount,
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

    public override Graphics.GraphicsAPI API => Graphics.GraphicsAPI.D3D11;

    public override SizeI ClientSize { get; protected set; }

    public override bool VSync { get; set; }

    public override Color4 ClearColor { get; set; } = Colors.CornflowerBlue;

    #region Window & Display State Notifications

    public override void OnResize(int width, int height)
    {
        ClientSize = new SizeI(width, height);

        _imContext.OMSetRenderTargets((ID3D11RenderTargetView)null!);

        // Release the old views, as they hold references to the buffers we
        // will be destroying. Also release the old depth/stencil buffer.
        _renderTargetView?.Dispose();
        _rtvBackBuffer?.Dispose();
        _depthStencilView?.Dispose();
        _depthStencilBuffer?.Dispose();

        // Resize the swap chain and recreate the render target view.
        _swapChain.ResizeBuffers(SwapChainBufferCount, (uint)width, (uint)height, _colorFormat, _swapChainFlags).CheckError();

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
    }

    public override void OnWindowMoved(RectI bounds) { }

    public override void OnWindowModeChanged(WindowMode mode) { }

    public override void OnMinimized() { }

    public override void OnRestored() { }

    #endregion

    #region Frame Lifecycle & Render Target Control

    public override bool BeginFrame()
    {
        ProcessDeferredDeletions();

        _currentPipeline = default;
        _currentViewport = default;
        _currentBlendConstantColor = default;
        _currentBindings = default;
        _currentVertexBuffer = default;
        _currentIndexBuffer = default;

        _imContext.ClearRenderTargetView(_renderTargetView, ClearColor);
        _imContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

        _imContext.OMSetRenderTargets(_renderTargetView, _depthStencilView);

        return true;
    }

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

        _currentFrameIndex++;
        return true;
    }

    public override void WaitIdle()
    {
        _imContext.Flush();
    }

    #endregion

    #region Resource Creation & Binding

    // Immutable States: cached & owned by GraphicsDevice until shutdown
    public override Graphics.InputLayoutHandle GetOrCreateInputLayout(in Graphics.VertexLayout layout, Graphics.ShaderHandle vs)
    {
        if (!_inputLayoutCache.TryGetValue(layout, out var handle))
        {
            ref readonly var shader = ref _shaders.Get(vs.Raw);
            var raw = _inputLayouts.Add(D3D11InputLayout.Create(_device, in layout, in shader));
            handle = new Graphics.InputLayoutHandle(raw);
            _inputLayoutCache[layout] = handle;
        }

        return handle;
    }

    public override Graphics.RasterStateHandle GetOrCreateRasterState(in Graphics.RasterStateDescription desc)
    {
        if (!_rasterStateCache.TryGetValue(desc, out var handle))
        {
            var raw = _rasterStates.Add(D3D11RasterState.Create(_device, in desc));
            handle = new Graphics.RasterStateHandle(raw);
            _rasterStateCache[desc] = handle;
        }

        return handle;
    }

    public override Graphics.SamplerHandle GetOrCreateSampler(in Graphics.SamplerDescription desc)
    {
        if (!_samplerCache.TryGetValue(desc, out var handle))
        {
            var raw = _samplers.Add(D3D11Sampler.Create(_device, in desc));
            handle = new Graphics.SamplerHandle(raw);
            _samplerCache[desc] = handle;
        }

        return handle;
    }

    public override Graphics.BlendStateHandle GetOrCreateBlendState(in Graphics.BlendStateDescription desc)
    {
        if (!_blendStateCache.TryGetValue(desc, out var handle))
        {
            var raw = _blendStates.Add(D3D11BlendState.Create(_device, in desc));
            handle = new Graphics.BlendStateHandle(raw);
            _blendStateCache[desc] = handle;
        }

        return handle;
    }

    public override Graphics.BindingLayoutHandle GetOrCreateBindingLayout(in Graphics.BindingLayoutDescription desc)
    {
        if (!_bindingLayoutCache.TryGetValue(desc, out var handle))
        {
            var raw = _bindingLayouts.Add(new D3D11BindingLayout(in desc));
            handle = new Graphics.BindingLayoutHandle(raw);
            _bindingLayoutCache[desc] = handle;
        }

        return handle;
    }

    public override Graphics.GraphicsPipelineHandle GetOrCreateGraphicsPipeline(in Graphics.GraphicsPipelineDescription desc)
    {
        if (!_pipelineCache.TryGetValue(desc, out var handle))
        {
            var raw = _pipelines.Add(D3D11GraphicsPipeline.Create(in desc, _shaders, _inputLayouts, _rasterStates, _blendStates));
            handle = new Graphics.GraphicsPipelineHandle(raw);
            _pipelineCache[desc] = handle;
        }

        return handle;
    }

    // Data Resources: owned by caller, destroyed via DeferredDeletionQueue
    public override Graphics.BufferHandle CreateBuffer<T>(ReadOnlySpan<T> data, Graphics.BufferDescription desc) =>
        new(_buffers.Add(D3D11Buffer.Create(_device, data, desc)));

    public override Graphics.BufferHandle CreateBuffer(Graphics.BufferDescription desc) =>
        new(_buffers.Add(D3D11Buffer.Create(_device, desc)));

    public override Graphics.TextureHandle CreateTexture<T>(ReadOnlySpan<T> data, in Graphics.TextureDescription desc) =>
        new(_textures.Add(D3D11Texture.Create(_device, _imContext, data, in desc)));

    public override Graphics.TextureHandle CreateTexture(in Graphics.TextureDescription desc) =>
        new(_textures.Add(D3D11Texture.Create(_device, in desc)));

    public override Graphics.ShaderHandle CreateShader(ReadOnlySpan<byte> source, in Graphics.ShaderDescription desc) =>
        new(_shaders.Add(D3D11Shader.Create(_device, source, in desc)));

    public override Graphics.BindingSetHandle CreateBindingSet(Graphics.BindingSetDescription desc, Graphics.BindingLayoutHandle layout)
    {
        var layoutData = _bindingLayouts.Get(layout.Raw);
#if GRAPHICS_DEBUG
        return new(_bindingSets.Add(D3D11BindingSet.Create(layout, desc, layoutData, _buffers, _textures, _samplers)));
#else
        return new(_bindingSets.Add(D3D11BindingSet.Create(desc, layoutData, _buffers, _textures, _samplers)));
#endif
    }

    public override Graphics.BufferDescription GetBufferDescription(Graphics.BufferHandle buffer) =>
        _buffers.Get(buffer.Raw).Desc;

    public override ref readonly Graphics.TextureDescription GetTextureDescription(Graphics.TextureHandle texture) =>
        ref _textures.Get(texture.Raw).Desc;

    public override unsafe void UpdateBuffer<T>(Graphics.BufferHandle buffer, ReadOnlySpan<T> data, uint byteOffset = 0)
    {
#if GRAPHICS_DEBUG
        if (data.IsEmpty)
        {
            throw new InvalidOperationException("Empty data received.");
        }
#endif

        ref readonly var bufferData = ref _buffers.Get(buffer.Raw);
        uint dataByteSize = (uint)(sizeof(T) * data.Length);

#if GRAPHICS_DEBUG
        if (byteOffset + dataByteSize > bufferData.Desc.ByteSize)
        {
            throw new InvalidOperationException("Data size exceeds buffer capacity.");
        }
#endif

        fixed (T* srcPtr = data)
        {
            MapMode mode = byteOffset == 0 ? MapMode.WriteDiscard : MapMode.WriteNoOverwrite;
            var map = _imContext.Map(bufferData.Native, 0, mode, Vortice.Direct3D11.MapFlags.None);

            byte* dstPtr = (byte*)map.DataPointer + byteOffset;
            Buffer.MemoryCopy(srcPtr, dstPtr, bufferData.Desc.ByteSize - byteOffset, dataByteSize);

            _imContext.Unmap(bufferData.Native, 0);
        }
    }

    public override unsafe void UpdateBuffer<T>(Graphics.BufferHandle buffer, in T data, uint byteOffset = 0)
    {
        ref readonly var bufferData = ref _buffers.Get(buffer.Raw);

#if GRAPHICS_DEBUG
        if (byteOffset + (uint)sizeof(T) > bufferData.Desc.ByteSize)
        {
            throw new InvalidOperationException("Data size exceeds buffer capacity.");
        }
#endif

        MapMode mode = byteOffset == 0 ? MapMode.WriteDiscard : MapMode.WriteNoOverwrite;
        var map = _imContext.Map(bufferData.Native, 0, mode, Vortice.Direct3D11.MapFlags.None);

        *(T*)((byte*)map.DataPointer + byteOffset) = data;

        _imContext.Unmap(bufferData.Native, 0);
    }

    public override unsafe void UpdateTexture<T>(Graphics.TextureHandle buffer, ReadOnlySpan<T> data, uint mipLevel = 0, uint arraySlice = 0)
    {
        ref readonly var textureData = ref _textures.Get(buffer.Raw);

#if GRAPHICS_DEBUG
        if (mipLevel >= textureData.Desc.MipLevels)
        {
            throw new InvalidOperationException("Mip level exceeds texture mip count.");
        }

        if (arraySlice >= textureData.Desc.ArraySize)
        {
            throw new InvalidOperationException("Array slice exceeds texture array size.");
        }
#endif

        uint mipWidth = Math.Max(1u, textureData.Desc.Width >> (int)mipLevel);
        uint mipHeight = Math.Max(1u, textureData.Desc.Height >> (int)mipLevel);
        uint mipDepth = Math.Max(1u, textureData.Desc.Depth >> (int)mipLevel);
        uint subresource = mipLevel + (arraySlice * textureData.Desc.MipLevels);
        Graphics.FormatInfo.GetSurfaceInfo(textureData.Desc.Format, mipWidth, mipHeight, out uint rowPitch, out uint slicePitch);

#if GRAPHICS_DEBUG
        uint expectedBytes = slicePitch * mipDepth;
        uint actualBytes = (uint)(sizeof(T) * data.Length);

        if (actualBytes < expectedBytes)
        {
            throw new InvalidOperationException($"Provided data size ({actualBytes} bytes) is smaller than the target mip level size ({expectedBytes} bytes).");
        }
#endif

        fixed (T* srcPtr = data)
        {
            _imContext.UpdateSubresource(textureData.Native, subresource, null, (nint)srcPtr, rowPitch, slicePitch);
        }
    }

    // Never frees immediately while GPU may use it; schedules into DeferredDeletionQueue
    public override void Destroy(Graphics.BufferHandle buffer)
    {
        if (!_buffers.TryRemove(buffer.Raw, out var bufferData))
        {
            Log.Warning(D3D11LogCategories.D3D11, $"Attempted to destroy invalid BufferHandle ({buffer.Index}:{buffer.Generation}).");
            return;
        }

        if (_currentVertexBuffer.Buffer == buffer) _currentVertexBuffer = default;
        if (_currentIndexBuffer.Buffer == buffer) _currentIndexBuffer = default;

        _deferredBuffers.Enqueue((_currentFrameIndex, bufferData));
    }

    public override void Destroy(Graphics.TextureHandle texture)
    {
        if (!_textures.TryRemove(texture.Raw, out var textureData))
        {
            Log.Warning(D3D11LogCategories.D3D11, $"Attempted to destroy invalid TextureHandle ({texture.Index}:{texture.Generation}).");
            return;
        }

        _deferredTextures.Enqueue((_currentFrameIndex, textureData));
    }

    public override void Destroy(Graphics.ShaderHandle shader)
    {
        if (!_shaders.TryRemove(shader.Raw, out var shaderData))
        {
            Log.Warning(D3D11LogCategories.D3D11, $"Attempted to destroy invalid ShaderHandle ({shader.Index}:{shader.Generation}).");
            return;
        }

        _deferredShaders.Enqueue((_currentFrameIndex, shaderData));
    }

    public override void Destroy(Graphics.BindingSetHandle bindingSet)
    {
        if (!_bindingSets.TryRemove(bindingSet.Raw, out _))
        {
            Log.Warning(D3D11LogCategories.D3D11, $"Attempted to destroy invalid BindingSetHandle ({bindingSet.Index}:{bindingSet.Generation}).");
            return;
        }

        for (int i = 0; i < Graphics.Constants.MaxBindingLayouts; i++)
        {
            if (_currentBindings[i] == bindingSet)
                _currentBindings[i] = default;
        }
    }

    public override void SetGraphicsState(in Graphics.GraphicsState state)
    {
#if GRAPHICS_DEBUG
        if (state.Pipeline.IsValid)
        {
            ref readonly var pipelineData = ref _pipelines.Get(state.Pipeline.Raw);
            for (int i = 0; i < Graphics.Constants.MaxBindingLayouts; i++)
            {
                var expectedLayout = pipelineData.BindingLayouts[i];
                var binding = state.Bindings[i];

                if (expectedLayout.IsValid)
                {
                    if (!binding.IsValid)
                    {
                        throw new InvalidOperationException($"Missing BindingSet at slot {i} required by GraphicsPipeline.");
                    }

                    ref readonly var setData = ref _bindingSets.Get(binding.Raw);
                    if (setData.Layout != expectedLayout)
                    {
                        throw new InvalidOperationException($"BindingSet at slot {i} does not match pipeline BindingLayout.");
                    }
                }
                else if (binding.IsValid)
                {
                    throw new InvalidOperationException($"Unexpected BindingSet at slot {i}: GraphicsPipeline does not declare a BindingLayout for this slot.");
                }
            }
        }
#endif

        bool pipelineChanged = _currentPipeline != state.Pipeline;
        bool blendColorChanged = _currentBlendConstantColor != state.BlendConstantColor;

        if (pipelineChanged)
        {
            if (state.Pipeline.IsValid)
            {
                _pipelines.Get(state.Pipeline.Raw).Apply(_imContext, state.BlendConstantColor);
            }
            _currentPipeline = state.Pipeline;
            _currentBlendConstantColor = state.BlendConstantColor;
        }
        else if (blendColorChanged && state.Pipeline.IsValid)
        {
            ref readonly var pipelineData = ref _pipelines.Get(state.Pipeline.Raw);
            if (pipelineData.UseBlendConstants)
            {
                _imContext.OMSetBlendState(pipelineData.BlendState, state.BlendConstantColor);
                _currentBlendConstantColor = state.BlendConstantColor;
            }
        }

        if ((state.Viewport.ViewportCount > 0 || state.Viewport.ScissorRectCount > 0) &&
            !_currentViewport.Equals(in state.Viewport))
        {
            ref readonly var vpState = ref state.Viewport;
            ref readonly var vpArr = ref vpState.Viewports;
            ref readonly var srArr = ref vpState.ScissorRects;

            if (vpState.ViewportCount == 1)
            {
                _imContext.RSSetViewport(vpArr[0]);
            }
            else if (vpState.ViewportCount > 1)
            {
                ref readonly var vp = ref vpArr[0];

                _imContext.RSSetViewports(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in vp), vpState.ViewportCount));
            }

            if (vpState.ScissorRectCount == 1)
            {
                _imContext.RSSetScissorRect(srArr[0]);
            }
            else if (vpState.ScissorRectCount > 1)
            {
                Span<Vortice.RawRect> rects = stackalloc Vortice.RawRect[vpState.ScissorRectCount];
                for (int i = 0; i < vpState.ScissorRectCount; i++)
                {
                    rects[i] = srArr[i];
                }
                _imContext.RSSetScissorRects(rects);
            }

            _currentViewport = state.Viewport;
        }

        for (int i = 0; i < Graphics.Constants.MaxBindingLayouts; i++)
        {
            var binding = state.Bindings[i];
            if (_currentBindings[i] != binding)
            {
                if (binding.IsValid)
                {
                    _bindingSets.Get(binding.Raw).Apply(_imContext);
                }
                _currentBindings[i] = binding;
            }
        }

        if (_currentVertexBuffer != state.VertexBuffer)
        {
            if (state.VertexBuffer.Buffer.IsValid)
            {
                ref readonly var vb = ref _buffers.Get(state.VertexBuffer.Buffer.Raw);
                _imContext.IASetVertexBuffer(state.VertexBuffer.Slot, vb.Native, vb.Desc.ByteStride, state.VertexBuffer.ByteOffset);
            }
            _currentVertexBuffer = state.VertexBuffer;
        }

        if (_currentIndexBuffer != state.IndexBuffer)
        {
            if (state.IndexBuffer.Buffer.IsValid)
            {
                ref readonly var ib = ref _buffers.Get(state.IndexBuffer.Buffer.Raw);
                _imContext.IASetIndexBuffer(ib.Native, state.IndexBuffer.Format.ToDXGIFormat(), state.IndexBuffer.ByteOffset);
            }
            _currentIndexBuffer = state.IndexBuffer;
        }
    }

    #endregion

    #region Drawing Commands (Direct, Instanced, Indirect)

    public override void Draw(uint vertexCount, uint startVertexLocation = 0)
    {
        _imContext.Draw(vertexCount, startVertexLocation);
    }

    public override void DrawIndexed(uint indexCount, uint startIndexLocation = 0, int baseVertexLocation = 0)
    {
        _imContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
    }

    public override void DrawInstanced(
        uint vertexCountPerInstance,
        uint instanceCount,
        uint startVertexLocation = 0,
        uint startInstanceLocation = 0)
    {
        _imContext.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
    }

    public override void DrawIndexedInstanced(
        uint indexCountPerInstance,
        uint instanceCount,
        uint startIndexLocation = 0,
        int baseVertexLocation = 0,
        uint startInstanceLocation = 0)
    {
        _imContext.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
    }

    public override void DrawIndirect(Graphics.BufferHandle buffer, uint alignedByteOffset = 0)
    {
        ref readonly var bufferData = ref _buffers.Get(buffer.Raw);
        _imContext.DrawInstancedIndirect(bufferData.Native, alignedByteOffset);
    }

    public override void DrawIndexedIndirect(Graphics.BufferHandle buffer, uint alignedByteOffset = 0)
    {
        ref readonly var bufferData = ref _buffers.Get(buffer.Raw);
        _imContext.DrawIndexedInstancedIndirect(bufferData.Native, alignedByteOffset);
    }

    #endregion

    #region Compute Commands

    public override void Dispatch(uint threadGroupCountX, uint threadGroupCountY = 1, uint threadGroupCountZ = 1)
    {
        _imContext.Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    public override void DispatchIndirect(Graphics.BufferHandle buffer, uint alignedByteOffset = 0)
    {
        _imContext.DispatchIndirect(_buffers.Get(buffer.Raw).Native, alignedByteOffset);
    }

    #endregion

    #region Disposing

    protected override void FreeResources()
    {
#if GRAPHICS_DEBUG
        if (_onFirstChanceException != null)
        {
            AppDomain.CurrentDomain.FirstChanceException -= _onFirstChanceException;
        }
#endif

        if (_imContext != null && _imContext.NativePointer != IntPtr.Zero)
        {
            _imContext.ClearState();
            _imContext.Flush();
        }

        // Flush all pending deferred deletions
        FlushDeferredDeletions();

        // Drain any leaked caller-owned resources
        _bindingSets.DrainAll(static (handle, res) =>
        {
            Log.Warning(D3D11LogCategories.D3D11, $"Leaked BindingSetHandle ({handle.Index}:{handle.Generation}) was not destroyed before device shutdown.");
            res.Destroy();
        });

        _buffers.DrainAll(static (handle, res) =>
        {
            Log.Warning(D3D11LogCategories.D3D11, $"Leaked BufferHandle ({handle.Index}:{handle.Generation}) was not destroyed before device shutdown.");
            res.Destroy();
        });

        _textures.DrainAll(static (handle, res) =>
        {
            Log.Warning(D3D11LogCategories.D3D11, $"Leaked TextureHandle ({handle.Index}:{handle.Generation}) was not destroyed before device shutdown.");
            res.Destroy();
        });

        _shaders.DrainAll(static (handle, res) =>
        {
            Log.Warning(D3D11LogCategories.D3D11, $"Leaked ShaderHandle ({handle.Index}:{handle.Generation}) was not destroyed before device shutdown.");
            res.Destroy();
        });

        // Free device-owned immutable state caches
        _pipelines.DrainAll(static (_, res) => res.Destroy());
        _pipelineCache.Clear();

        _bindingLayouts.DrainAll(static (_, res) => res.Destroy());
        _bindingLayoutCache.Clear();

        _inputLayouts.DrainAll(static (_, res) => res.Destroy());
        _inputLayoutCache.Clear();

        _rasterStates.DrainAll(static (_, res) => res.Destroy());
        _rasterStateCache.Clear();

        _samplers.DrainAll(static (_, res) => res.Destroy());
        _samplerCache.Clear();

        _blendStates.DrainAll(static (_, res) => res.Destroy());
        _blendStateCache.Clear();

        // Release main interfaces and debug leak reports
        _depthStencilView?.Dispose();
        _depthStencilBuffer?.Dispose();
        _renderTargetView?.Dispose();
        _rtvBackBuffer?.Dispose();

        _imContext?.Dispose();
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

    private void ProcessDeferredDeletions()
    {
        while (_deferredBuffers.TryPeek(out var entry) && _currentFrameIndex >= entry.Frame + FramesInFlight)
        {
            _deferredBuffers.Dequeue().Resource.Destroy();
        }
        while (_deferredTextures.TryPeek(out var entry) && _currentFrameIndex >= entry.Frame + FramesInFlight)
        {
            _deferredTextures.Dequeue().Resource.Destroy();
        }
        while (_deferredShaders.TryPeek(out var entry) && _currentFrameIndex >= entry.Frame + FramesInFlight)
        {
            _deferredShaders.Dequeue().Resource.Destroy();
        }
    }

    private void FlushDeferredDeletions()
    {
        while (_deferredBuffers.TryDequeue(out var entry)) entry.Resource.Destroy();
        while (_deferredTextures.TryDequeue(out var entry)) entry.Resource.Destroy();
        while (_deferredShaders.TryDequeue(out var entry)) entry.Resource.Destroy();
    }

#if GRAPHICS_DEBUG
    private static LogSeverity ToLogSeverity(MessageSeverity severity) => severity switch
    {
        MessageSeverity.Corruption => LogSeverity.Fatal,
        MessageSeverity.Error => LogSeverity.Error,
        MessageSeverity.Warning => LogSeverity.Warning,
        MessageSeverity.Info => LogSeverity.Info,
        _ => LogSeverity.Message
    };

    private static LogSeverity ToLogSeverity(InfoQueueMessageSeverity severity) => severity switch
    {
        InfoQueueMessageSeverity.Corruption => LogSeverity.Fatal,
        InfoQueueMessageSeverity.Error => LogSeverity.Error,
        InfoQueueMessageSeverity.Warning => LogSeverity.Warning,
        InfoQueueMessageSeverity.Info => LogSeverity.Info,
        _ => LogSeverity.Message
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
        foreach (var leak in CollectionsMarshal.AsSpan(leaks))
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
