using Vortice.Direct3D11;

using TortGfx = Torture.Graphics;

namespace Torture.Direct3D11;

internal sealed class D3D11Buffer : TortGfx.Buffer
{
    private D3D11Buffer(ID3D11Buffer buffer, ID3D11ShaderResourceView? srv = null, ID3D11UnorderedAccessView? uav = null)
    {
        Native = buffer;
        Srv = srv;
        Uav = uav;
    }

    public ID3D11Buffer Native { get; private set; }

    public ID3D11ShaderResourceView? Srv { get; private set; }

    public ID3D11UnorderedAccessView? Uav { get; private set; }

    public static unsafe D3D11Buffer Create<T>(ID3D11Device5 device, ReadOnlySpan<T> data, in TortGfx.BufferDescription desc)
        where T : unmanaged
    {
        BufferDescription nativeDesc = ToNativeBufferDescription(desc, true);

        uint vertexByteSize = (uint)sizeof(T);

        uint byteSize = desc.ByteSize > 0 ? desc.ByteSize : vertexByteSize * (uint)data.Length;
        uint byteStride = desc.ByteStride > 0 ? desc.ByteStride : vertexByteSize;

        uint structureByteStride =
            (nativeDesc.MiscFlags & ResourceOptionFlags.BufferStructured) == ResourceOptionFlags.BufferStructured
            ? byteStride
            : 0;

        nativeDesc.ByteWidth = byteSize;
        nativeDesc.StructureByteStride = structureByteStride;

        ID3D11Buffer buffer = default!;

        fixed (T* dataPtr = data)
        {
            SubresourceData initialData = new((nint)dataPtr);
            buffer = device.CreateBuffer(nativeDesc, initialData);
        }

        return new D3D11Buffer(buffer)
        {
            ByteSize = byteSize,
            ByteStride = byteStride
        };
    }

    public static D3D11Buffer Create(ID3D11Device5 device, in TortGfx.BufferDescription desc)
    {
        BufferDescription nativeDesc = ToNativeBufferDescription(desc, false);

        uint structureByteStride =
            (nativeDesc.MiscFlags & ResourceOptionFlags.BufferStructured) == ResourceOptionFlags.BufferStructured
            ? desc.ByteStride
            : 0;

        nativeDesc.ByteWidth = desc.ByteSize;
        nativeDesc.StructureByteStride = structureByteStride;

        ID3D11Buffer buffer = device.CreateBuffer(nativeDesc, initialData: null);

        return new D3D11Buffer(buffer)
        {
            ByteSize = desc.ByteSize,
            ByteStride = desc.ByteStride
        };
    }

    private static BufferDescription ToNativeBufferDescription(in TortGfx.BufferDescription desc, bool hasInitialData)
    {
        ResourceUsage resourceUsage;
        CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None;

        switch (desc.CpuAccess)
        {
            case TortGfx.CpuAccess.None:
                // If we have an initial data, we can set Immutable for best performance
                resourceUsage = hasInitialData ? ResourceUsage.Immutable : ResourceUsage.Default;
                break;
            case TortGfx.CpuAccess.Write:
                resourceUsage = ResourceUsage.Dynamic;
                cpuAccessFlags = CpuAccessFlags.Write;
                break;
            case TortGfx.CpuAccess.Read:
                resourceUsage = ResourceUsage.Staging;
                cpuAccessFlags = CpuAccessFlags.Read;
                break;
            default:
                throw new NotSupportedException();
        }

        // If cpu access is read, then d3d11 resource usage is staging, for staging buffers BindFlags should be None
        BindFlags bindFlags = BindFlags.None;
        if (desc.CpuAccess != TortGfx.CpuAccess.Read)
        {
            if ((desc.Usage & TortGfx.BufferUsage.VertexBuffer) == TortGfx.BufferUsage.VertexBuffer) bindFlags |= BindFlags.VertexBuffer;
            if ((desc.Usage & TortGfx.BufferUsage.IndexBuffer) == TortGfx.BufferUsage.IndexBuffer) bindFlags |= BindFlags.IndexBuffer;
            if ((desc.Usage & TortGfx.BufferUsage.ConstantBuffer) == TortGfx.BufferUsage.ConstantBuffer) bindFlags |= BindFlags.ConstantBuffer;
            if ((desc.Usage & TortGfx.BufferUsage.ShaderResource) == TortGfx.BufferUsage.ShaderResource ||
                (desc.Usage & TortGfx.BufferUsage.StructuredBuffer) == TortGfx.BufferUsage.StructuredBuffer) bindFlags |= BindFlags.ShaderResource;
            if ((desc.Usage & TortGfx.BufferUsage.UnorderedAccess) == TortGfx.BufferUsage.UnorderedAccess) bindFlags |= BindFlags.UnorderedAccess;
        }

        ResourceOptionFlags miscFlags = ResourceOptionFlags.None;
        if ((desc.Usage & TortGfx.BufferUsage.IndirectArguments) == TortGfx.BufferUsage.IndirectArguments)
        {
            miscFlags |= ResourceOptionFlags.DrawIndirectArguments;
        }

        bool isStructured = (desc.Usage & TortGfx.BufferUsage.StructuredBuffer) == TortGfx.BufferUsage.StructuredBuffer;
        if (isStructured)
        {
            miscFlags |= ResourceOptionFlags.BufferStructured;
        }

        if ((desc.Usage & TortGfx.BufferUsage.RawBuffer) == TortGfx.BufferUsage.RawBuffer)
        {
            miscFlags |= ResourceOptionFlags.BufferAllowRawViews;
        }

        return new BufferDescription
        {
            ByteWidth = 0,
            Usage = resourceUsage,
            BindFlags = bindFlags,
            CPUAccessFlags = cpuAccessFlags,
            MiscFlags = miscFlags,
            StructureByteStride = 0
        };
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        Srv?.Dispose();
        Uav?.Dispose();
        Native?.Dispose();

        _disposed = true;
    }
}
