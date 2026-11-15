using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Debug;
using Vortice.DXGI;
using Vortice.DXGI.Debug;
using Vortice.Mathematics;

using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

using CoreGfx = Torture.Core.Graphics;

namespace Torture.Graphics.Direct3D11;

/// <inheritdoc />
public sealed class D3D11GraphicsDevice : CoreGfx.GraphicsDevice
{
    private static readonly FeatureLevel[] FeatureLevels = [
        FeatureLevel.Level_11_1,
        FeatureLevel.Level_11_0];

    private const int BufferCount = 2;

    private readonly bool _tearingSupported;
    private readonly SwapChainFlags _swapChainFlags;
    private readonly Format _colorFormat;
    private readonly Format _depthStencilFormat;

    private readonly IDXGIFactory7 _factory;
    private readonly ID3D11Device5 _device;
    private readonly ID3D11DeviceContext4 _imContext;
    private readonly IDXGISwapChain1 _swapChain;
    private readonly ID3D11InfoQueue? _infoQueue;
    private ID3D11Texture2D _rtvBackBuffer = null!;
    private ID3D11RenderTargetView _renderTargetView = null!;
    private ID3D11Texture2D _depthStencilBuffer = null!;
    private ID3D11DepthStencilView _depthStencilView = null!;

    public D3D11GraphicsDevice(CoreGfx.GraphicsDeviceOptions options,
        nint hwnd, SizeI clientSize, Format colorFormat = Format.B8G8R8A8_UNorm, Format depthStencilFormat = Format.D24_UNorm_S8_UInt)
        : base(options)
    {
        // Global SharpGen switch: must be set before the first COM wrapper is created.
        if (options.EnableObjectTracking)
        {
            SharpGen.Runtime.Configuration.EnableObjectTracking = true;
        }

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

                if (options.EnableValidation)
                {
                    if (SdkLayersAvailable())
                    {
                        deviceCreationFlags |= DeviceCreationFlags.Debug;
                    }
                    else
                    {
                        Core.Diagnostics.Log.Warning("D3D11", "Validation requested, but D3D11 SDK layers are not installed " +
                                             "(Settings > System > Optional features > Graphics Tools).");
                    }
                }

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

            // QueryInterface returns null when the device was created without the debug layer.
            _infoQueue = options.EnableValidation ? _device.QueryInterfaceOrNull<ID3D11InfoQueue>() : null;
            if (_infoQueue != null)
            {
                Core.Diagnostics.Log.Info("D3D11", "Debug layer enabled.");
                if (options.BreakOnError && System.Diagnostics.Debugger.IsAttached)
                {
                    _infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
                    _infoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
                }
            }

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

            // Handle color space settings for HDR
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
    public override CoreGfx.GraphicsAPI API => CoreGfx.GraphicsAPI.D3D11;

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

        // Set the viewport transform.
        _imContext.RSSetViewport(0, 0, (float)width, (float)height, 0.0f, 1.0f);
    }

    /// <inheritdoc />
    public override void OnWindowMoved(RectI bounds) { }

    /// <inheritdoc />
    public override void OnWindowModeChanged(Core.WindowMode mode) { }

    /// <inheritdoc />
    public override void OnMinimized() { }

    /// <inheritdoc />
    public override void OnRestored() { }

    #endregion

    #region Frame Lifecycle & Render Target Control

    /// <inheritdoc />
    public override bool BeginFrame()
    {
        _imContext.ClearRenderTargetView(_renderTargetView, ClearColor);
        _imContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

        _imContext.OMSetRenderTargets(_renderTargetView, _depthStencilView);
        _imContext.RSSetViewport(Viewport);
        _imContext.RSSetScissorRect(ScissorRect);

        return true;
    }

    /// <inheritdoc />
    public override void Clear()
    {
        _imContext.ClearRenderTargetView(_renderTargetView, ClearColor);
        _imContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
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
        _swapChain.Present(syncInterval, presentFlags).CheckError();
#else
        _swapChain.Present(syncInterval, presentFlags);
#endif
        FlushDebugMessages();
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
    public override CoreGfx.Buffer CreateBuffer<T>(ReadOnlySpan<T> data, in CoreGfx.BufferDescription desc)
    {
        return D3D11Buffer.Create(_device, data, in desc);
    }

    /// <inheritdoc />
    public override CoreGfx.Buffer CreateBuffer(in CoreGfx.BufferDescription desc)
    {
        return D3D11Buffer.Create(_device, in desc);
    }

    /// <inheritdoc />
    public override CoreGfx.Shader CreateShader(in CoreGfx.ShaderDescription desc)
    {
        return new D3D11Shader(_device, in desc);
    }

    /// <inheritdoc />
    public override CoreGfx.InputLayout CreateInputLayout(in CoreGfx.VertexLayout layout, CoreGfx.Shader shader)
    {
        return new D3D11InputLayout(_device, in layout, (D3D11Shader)shader);
    }

    /// <inheritdoc />
    public override CoreGfx.Texture CreateTexture<T>(ReadOnlySpan<T> data, in CoreGfx.TextureDescription desc)
    {
        return D3D11Texture.Create(_device, data, in desc);
    }

    /// <inheritdoc />
    public override CoreGfx.Texture CreateTexture(in CoreGfx.TextureDescription desc)
    {
        return D3D11Texture.Create(_device, in desc);
    }

    /// <inheritdoc />
    public override CoreGfx.Sampler CreateSampler(in CoreGfx.SamplerDescription desc)
    {
        return new D3D11Sampler(_device, in desc);
    }

    /// <inheritdoc />
    public override unsafe void UpdateBuffer<T>(CoreGfx.Buffer buffer, ReadOnlySpan<T> data, uint byteOffset = 0)
    {
#if GRAPHICS_DEBUG
        if (data.IsEmpty)
        {
            throw new ArgumentException("Data was empty.", nameof(data));
        }
#endif

        var d3dBuffer = (D3D11Buffer)buffer;
        uint dataByteSize = (uint)(sizeof(T) * data.Length);

#if GRAPHICS_DEBUG
        if (byteOffset + dataByteSize > d3dBuffer.ByteSize)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Data size exceeds buffer capacity.");
        }
#endif

        fixed (T* srcPtr = data)
        {
            MapMode mode = byteOffset == 0 ? MapMode.WriteDiscard : MapMode.WriteNoOverwrite;
            var map = _imContext.Map(d3dBuffer.Native, 0, mode, Vortice.Direct3D11.MapFlags.None);

            byte* dstPtr = (byte*)map.DataPointer + byteOffset;
            Buffer.MemoryCopy(srcPtr, dstPtr, d3dBuffer.ByteSize - byteOffset, dataByteSize);

            _imContext.Unmap(d3dBuffer.Native, 0);
        }
    }

    /// <inheritdoc />
    public override unsafe void UpdateBuffer<T>(CoreGfx.Buffer buffer, ref T data, uint byteOffset = 0)
    {
        var d3dBuffer = (D3D11Buffer)buffer;

#if GRAPHICS_DEBUG
        if (byteOffset + (uint)sizeof(T) > d3dBuffer.ByteSize)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Data size exceeds buffer capacity.");
        }
#endif

        MapMode mode = byteOffset == 0 ? MapMode.WriteDiscard : MapMode.WriteNoOverwrite;
        var map = _imContext.Map(d3dBuffer.Native, 0, mode, Vortice.Direct3D11.MapFlags.None);

        *(T*)((byte*)map.DataPointer + byteOffset) = data;

        _imContext.Unmap(d3dBuffer.Native, 0);
    }

    /// <inheritdoc />
    public override unsafe void UpdateTexture<T>(CoreGfx.Texture texture, ReadOnlySpan<T> data, uint mipLevel = 0, uint arraySlice = 0)
    {
        var d3dTexture = (D3D11Texture)texture;

        uint mipWidth = Math.Max(1u, d3dTexture.Width >> (int)mipLevel);
        uint mipHeight = Math.Max(1u, d3dTexture.Height >> (int)mipLevel);
        uint subresource = mipLevel + (arraySlice * d3dTexture.MipLevels);
        CoreGfx.FormatInfo.GetSurfaceInfo(d3dTexture.Format, mipWidth, mipHeight, out uint rowPitch, out uint slicePitch);

        fixed (T* srcPtr = data)
        {
            _imContext.UpdateSubresource(d3dTexture.Native, subresource, null, (nint)srcPtr, rowPitch, slicePitch);
        }
    }

    /// <inheritdoc />
    public override void SetPrimitiveTopology(CoreGfx.PrimitiveTopology primitiveTopology)
    {
        _imContext.IASetPrimitiveTopology(primitiveTopology.ToD3D11PrimitiveTopology());
    }

    /// <inheritdoc />
    public override void SetVertexBuffer(CoreGfx.Buffer buffer, uint slot = 0, uint byteOffset = 0)
    {
        var d3dBuffer = (D3D11Buffer)buffer;
        _imContext.IASetVertexBuffer(slot, d3dBuffer.Native, d3dBuffer.ByteStride, byteOffset);
    }

    /// <inheritdoc />
    public override void SetIndexBuffer(CoreGfx.Buffer buffer, uint byteOffset = 0)
    {
        var d3dBuffer = (D3D11Buffer)buffer;
        _imContext.IASetIndexBuffer(d3dBuffer.Native, ((CoreGfx.IndexFormat)d3dBuffer.ByteStride).ToDXGIFormat(), byteOffset);
    }

    /// <inheritdoc />
    public override void SetConstantBuffer(CoreGfx.Buffer buffer, CoreGfx.ShaderType stage, uint slot)
    {
        var d3dBuffer = (D3D11Buffer)buffer;

        if ((stage & CoreGfx.ShaderType.Vertex) == CoreGfx.ShaderType.Vertex)
        {
            _imContext.VSSetConstantBuffer(slot, d3dBuffer.Native);
        }
        if ((stage & CoreGfx.ShaderType.Hull) == CoreGfx.ShaderType.Hull)
        {
            _imContext.HSSetConstantBuffer(slot, d3dBuffer.Native);
        }
        if ((stage & CoreGfx.ShaderType.Domain) == CoreGfx.ShaderType.Domain)
        {
            _imContext.DSSetConstantBuffer(slot, d3dBuffer.Native);
        }
        if ((stage & CoreGfx.ShaderType.Geometry) == CoreGfx.ShaderType.Geometry)
        {
            _imContext.GSSetConstantBuffer(slot, d3dBuffer.Native);
        }
        if ((stage & CoreGfx.ShaderType.Pixel) == CoreGfx.ShaderType.Pixel)
        {
            _imContext.PSSetConstantBuffer(slot, d3dBuffer.Native);
        }
        if ((stage & CoreGfx.ShaderType.Compute) == CoreGfx.ShaderType.Compute)
        {
            _imContext.CSSetConstantBuffer(slot, d3dBuffer.Native);
        }
    }

    /// <inheritdoc />
    public override void SetVertexShader(CoreGfx.Shader shader)
    {
        _imContext.VSSetShader((ID3D11VertexShader)((D3D11Shader)shader).Native);
    }

    /// <inheritdoc />
    public override void SetPixelShader(CoreGfx.Shader shader)
    {
        _imContext.PSSetShader((ID3D11PixelShader)((D3D11Shader)shader).Native);
    }

    /// <inheritdoc />
    public override void SetInputLayout(CoreGfx.InputLayout inputLayout)
    {
        _imContext.IASetInputLayout(((D3D11InputLayout)inputLayout).Native);
    }

    /// <inheritdoc />
    public override void SetTexture(CoreGfx.Texture texture, CoreGfx.ShaderType stage, uint slot)
    {
        var d3dTexture = (D3D11Texture)texture;

        if ((stage & CoreGfx.ShaderType.Vertex) == CoreGfx.ShaderType.Vertex)
        {
            _imContext.VSSetShaderResource(slot, d3dTexture.Srv);
        }
        if ((stage & CoreGfx.ShaderType.Hull) == CoreGfx.ShaderType.Hull)
        {
            _imContext.HSSetShaderResource(slot, d3dTexture.Srv);
        }
        if ((stage & CoreGfx.ShaderType.Domain) == CoreGfx.ShaderType.Domain)
        {
            _imContext.DSSetShaderResource(slot, d3dTexture.Srv);
        }
        if ((stage & CoreGfx.ShaderType.Geometry) == CoreGfx.ShaderType.Geometry)
        {
            _imContext.GSSetShaderResource(slot, d3dTexture.Srv);
        }
        if ((stage & CoreGfx.ShaderType.Pixel) == CoreGfx.ShaderType.Pixel)
        {
            _imContext.PSSetShaderResource(slot, d3dTexture.Srv!);
        }
        if ((stage & CoreGfx.ShaderType.Compute) == CoreGfx.ShaderType.Compute)
        {
            _imContext.CSSetShaderResource(slot, d3dTexture.Srv);
        }
    }

    /// <inheritdoc />
    public override void SetSampler(CoreGfx.Sampler sampler, CoreGfx.ShaderType stage, uint slot)
    {
        var d3dSampler = (D3D11Sampler)sampler;

        if ((stage & CoreGfx.ShaderType.Vertex) == CoreGfx.ShaderType.Vertex)
        {
            _imContext.VSSetSampler(slot, d3dSampler.Native);
        }
        if ((stage & CoreGfx.ShaderType.Hull) == CoreGfx.ShaderType.Hull)
        {
            _imContext.HSSetSampler(slot, d3dSampler.Native);
        }
        if ((stage & CoreGfx.ShaderType.Domain) == CoreGfx.ShaderType.Domain)
        {
            _imContext.DSSetSampler(slot, d3dSampler.Native);
        }
        if ((stage & CoreGfx.ShaderType.Geometry) == CoreGfx.ShaderType.Geometry)
        {
            _imContext.GSSetSampler(slot, d3dSampler.Native);
        }
        if ((stage & CoreGfx.ShaderType.Pixel) == CoreGfx.ShaderType.Pixel)
        {
            _imContext.PSSetSampler(slot, d3dSampler.Native);
        }
        if ((stage & CoreGfx.ShaderType.Compute) == CoreGfx.ShaderType.Compute)
        {
            _imContext.CSSetSampler(slot, d3dSampler.Native);
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
    public override void DrawIndirect(CoreGfx.Buffer buffer, uint alignedByteOffset = 0)
    {
        _imContext.DrawInstancedIndirect(((D3D11Buffer)buffer).Native, alignedByteOffset);
    }

    /// <inheritdoc />
    public override void DrawIndexedIndirect(CoreGfx.Buffer buffer, uint alignedByteOffset = 0)
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
    public override void DispatchIndirect(CoreGfx.Buffer buffer, uint alignedByteOffset = 0)
    {
        _imContext.DispatchIndirect(((D3D11Buffer)buffer).Native, alignedByteOffset);
    }

    #endregion

    #region Disposing

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_disposed)
            return;

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
        _device?.Dispose();
        _factory?.Dispose();

        if (Options.EnableValidation)
        {
            ReportDxgiLiveObjects();
        }
        if (Options.EnableObjectTracking)
        {
            ReportSharpGenLeaks();
        }

        _disposed = true;
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
        // TBD:
    }

    private void FlushDebugMessages()
    {
        if (_infoQueue is null || _infoQueue.NativePointer == nint.Zero)
        {
            return;
        }

        ulong count = _infoQueue.NumStoredMessages;
        for (ulong i = 0; i < count; i++)
        {
            Message msg = _infoQueue.GetMessage(i);
            switch (msg.Severity)
            {
                case MessageSeverity.Corruption:
                case MessageSeverity.Error:
                    Core.Diagnostics.Log.Error("D3D11", $"({msg.Id}): {msg.Description}");
                    break;
                case MessageSeverity.Warning:
                    Core.Diagnostics.Log.Warning("D3D11", $"({msg.Id}): {msg.Description}");
                    break;
                default:
                    Core.Diagnostics.Log.Info("D3D11", $"({msg.Id}): {msg.Description}");
                    break;
            }
        }

        _infoQueue.ClearStoredMessages();
    }

    private static void ReportDxgiLiveObjects()
    {
        if (DXGIGetDebugInterface1(out IDXGIDebug1? dxgiDebug).Failure || dxgiDebug is null)
        {
            return;
        }

        if (DXGIGetDebugInterface1(out IDXGIInfoQueue? dxgiInfoQueue).Success && dxgiInfoQueue != null)
        {
            dxgiInfoQueue.ClearStoredMessages(DebugAll);
            dxgiDebug.ReportLiveObjects(DebugAll, ReportLiveObjectFlags.Detail | ReportLiveObjectFlags.IgnoreInternal);

            ulong count = dxgiInfoQueue.GetNumStoredMessages(DebugAll);
            for (ulong i = 0; i < count; i++)
            {
                InfoQueueMessage msg = dxgiInfoQueue.GetMessage(DebugAll, i);
                Core.Diagnostics.Log.Warning("DXGI", $"{msg.Severity} ({msg.Id}): {msg.Description}");
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
                // Same style as D3D11/DXGI output: [SharpGen Warning] (0x000001DF...): Live instance of ...
                Core.Diagnostics.Log.Warning("SharpGen", $"(0x{target.NativePointer:X16}): Live instance of {target.GetType().Name} was not disposed.");
            }
            else
            {
                Core.Diagnostics.Log.Warning("SharpGen", "Active tracking reference with an already collected target.");
            }
        }
    }

    #endregion
}
