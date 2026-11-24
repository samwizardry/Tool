using Vortice.Direct3D11;

namespace Tool.Direct3D11;

internal readonly struct D3D11Buffer
{
    public readonly Graphics.BufferDescription Desc;
    public readonly ID3D11Buffer Native;
    public readonly ID3D11ShaderResourceView? Srv;
    public readonly ID3D11UnorderedAccessView? Uav;

    private D3D11Buffer(in Graphics.BufferDescription desc, ID3D11Buffer buffer, ID3D11ShaderResourceView? srv = null, ID3D11UnorderedAccessView? uav = null)
    {
        Desc = desc;
        Native = buffer;
        Srv = srv;
        Uav = uav;
    }

    public void Destroy()
    {
        Srv?.Dispose();
        Uav?.Dispose();
        Native?.Dispose();
    }

    public static unsafe D3D11Buffer Create<T>(ID3D11Device5 device, ReadOnlySpan<T> data, in Graphics.BufferDescription desc)
        where T : unmanaged
    {
        BufferDescription nativeDesc = ToNativeBufferDescription(in desc, true);

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

        Graphics.BufferDescription resolvedDesc = desc with
        {
            ByteSize = byteSize,
            ByteStride = byteStride
        };

        return new D3D11Buffer(in resolvedDesc, buffer);
    }

    public static D3D11Buffer Create(ID3D11Device5 device, in Graphics.BufferDescription desc)
    {
        BufferDescription nativeDesc = ToNativeBufferDescription(desc, false);

        uint structureByteStride =
            (nativeDesc.MiscFlags & ResourceOptionFlags.BufferStructured) == ResourceOptionFlags.BufferStructured
            ? desc.ByteStride
            : 0;

        nativeDesc.ByteWidth = desc.ByteSize;
        nativeDesc.StructureByteStride = structureByteStride;

        ID3D11Buffer buffer = device.CreateBuffer(nativeDesc, initialData: null);

        return new D3D11Buffer(in desc, buffer);
    }

    private static BufferDescription ToNativeBufferDescription(in Graphics.BufferDescription desc, bool hasInitialData)
    {
        ResourceUsage resourceUsage;
        CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None;

        switch (desc.CpuAccess)
        {
            case Graphics.CpuAccess.None:
                // If we have an initial data, we can set Immutable for best performance
                resourceUsage = hasInitialData ? ResourceUsage.Immutable : ResourceUsage.Default;
                break;
            case Graphics.CpuAccess.Write:
                resourceUsage = ResourceUsage.Dynamic;
                cpuAccessFlags = CpuAccessFlags.Write;
                break;
            case Graphics.CpuAccess.Read:
                resourceUsage = ResourceUsage.Staging;
                cpuAccessFlags = CpuAccessFlags.Read;
                break;
            default:
                throw new NotSupportedException();
        }

        // If cpu access is read, then d3d11 resource usage is staging, for staging buffers BindFlags should be None
        BindFlags bindFlags = BindFlags.None;
        if (desc.CpuAccess != Graphics.CpuAccess.Read)
        {
            if ((desc.Usage & Graphics.BufferUsage.VertexBuffer) == Graphics.BufferUsage.VertexBuffer) bindFlags |= BindFlags.VertexBuffer;
            if ((desc.Usage & Graphics.BufferUsage.IndexBuffer) == Graphics.BufferUsage.IndexBuffer) bindFlags |= BindFlags.IndexBuffer;
            if ((desc.Usage & Graphics.BufferUsage.ConstantBuffer) == Graphics.BufferUsage.ConstantBuffer) bindFlags |= BindFlags.ConstantBuffer;
            if ((desc.Usage & Graphics.BufferUsage.ShaderResource) == Graphics.BufferUsage.ShaderResource ||
                (desc.Usage & Graphics.BufferUsage.StructuredBuffer) == Graphics.BufferUsage.StructuredBuffer) bindFlags |= BindFlags.ShaderResource;
            if ((desc.Usage & Graphics.BufferUsage.UnorderedAccess) == Graphics.BufferUsage.UnorderedAccess) bindFlags |= BindFlags.UnorderedAccess;
        }

        ResourceOptionFlags miscFlags = ResourceOptionFlags.None;
        if ((desc.Usage & Graphics.BufferUsage.IndirectArguments) == Graphics.BufferUsage.IndirectArguments)
        {
            miscFlags |= ResourceOptionFlags.DrawIndirectArguments;
        }

        bool isStructured = (desc.Usage & Graphics.BufferUsage.StructuredBuffer) == Graphics.BufferUsage.StructuredBuffer;
        if (isStructured)
        {
            miscFlags |= ResourceOptionFlags.BufferStructured;
        }

        if ((desc.Usage & Graphics.BufferUsage.RawBuffer) == Graphics.BufferUsage.RawBuffer)
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
}
