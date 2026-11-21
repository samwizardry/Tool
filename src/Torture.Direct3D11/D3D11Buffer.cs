using Vortice.Direct3D11;

namespace Torture.Direct3D11;

internal sealed class D3D11Buffer : CoreBuffer
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

    public static unsafe D3D11Buffer Create<T>(ID3D11Device5 device, ReadOnlySpan<T> data, in CoreBufferDesc desc)
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

    public static D3D11Buffer Create(ID3D11Device5 device, in CoreBufferDesc desc)
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

    private static BufferDescription ToNativeBufferDescription(in CoreBufferDesc desc, bool hasInitialData)
    {
        ResourceUsage resourceUsage;
        CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None;

        switch (desc.CpuAccess)
        {
            case CoreCpuAccess.None:
                // If we have an initial data, we can set Immutable for best performance
                resourceUsage = hasInitialData ? ResourceUsage.Immutable : ResourceUsage.Default;
                break;
            case CoreCpuAccess.Write:
                resourceUsage = ResourceUsage.Dynamic;
                cpuAccessFlags = CpuAccessFlags.Write;
                break;
            case CoreCpuAccess.Read:
                resourceUsage = ResourceUsage.Staging;
                cpuAccessFlags = CpuAccessFlags.Read;
                break;
            default:
                throw new NotSupportedException();
        }

        // If cpu access is read, then d3d11 resource usage is staging, for staging buffers BindFlags should be None
        BindFlags bindFlags = BindFlags.None;
        if (desc.CpuAccess != CoreCpuAccess.Read)
        {
            if ((desc.Usage & CoreBufferUsage.VertexBuffer) == CoreBufferUsage.VertexBuffer) bindFlags |= BindFlags.VertexBuffer;
            if ((desc.Usage & CoreBufferUsage.IndexBuffer) == CoreBufferUsage.IndexBuffer) bindFlags |= BindFlags.IndexBuffer;
            if ((desc.Usage & CoreBufferUsage.ConstantBuffer) == CoreBufferUsage.ConstantBuffer) bindFlags |= BindFlags.ConstantBuffer;
            if ((desc.Usage & CoreBufferUsage.ShaderResource) == CoreBufferUsage.ShaderResource ||
                (desc.Usage & CoreBufferUsage.StructuredBuffer) == CoreBufferUsage.StructuredBuffer) bindFlags |= BindFlags.ShaderResource;
            if ((desc.Usage & CoreBufferUsage.UnorderedAccess) == CoreBufferUsage.UnorderedAccess) bindFlags |= BindFlags.UnorderedAccess;
        }

        ResourceOptionFlags miscFlags = ResourceOptionFlags.None;
        if ((desc.Usage & CoreBufferUsage.IndirectArguments) == CoreBufferUsage.IndirectArguments)
        {
            miscFlags |= ResourceOptionFlags.DrawIndirectArguments;
        }

        bool isStructured = (desc.Usage & CoreBufferUsage.StructuredBuffer) == CoreBufferUsage.StructuredBuffer;
        if (isStructured)
        {
            miscFlags |= ResourceOptionFlags.BufferStructured;
        }

        if ((desc.Usage & CoreBufferUsage.RawBuffer) == CoreBufferUsage.RawBuffer)
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
