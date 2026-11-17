using Vortice.Direct3D11;

using CoreGfx = Torture.Core.Graphics;

namespace Torture.Graphics.Direct3D11;

internal sealed class D3D11Buffer : CoreGfx.Buffer
{
    private D3D11Buffer(ID3D11Buffer buffer)
    {
        Native = buffer;
    }

    public ID3D11Buffer Native { get; private set; }

    public static unsafe D3D11Buffer Create<T>(ID3D11Device5 device, ReadOnlySpan<T> data, in CoreGfx.BufferDescription desc)
        where T : unmanaged
    {
        BufferDescription d3dDesc = ToD3DBufferDescription(desc, true);

        uint vertexByteSize = (uint)sizeof(T);

        uint byteSize = d3dDesc.ByteWidth > 0 ? d3dDesc.ByteWidth : vertexByteSize * (uint)data.Length;
        uint byteStride = d3dDesc.StructureByteStride > 0 ? d3dDesc.StructureByteStride : vertexByteSize;

        bool isStructured = (d3dDesc.MiscFlags & ResourceOptionFlags.BufferStructured) == ResourceOptionFlags.BufferStructured;
        uint structureByteStride = isStructured ? byteStride : 0;

        d3dDesc.ByteWidth = byteSize;
        d3dDesc.StructureByteStride = structureByteStride;

        ID3D11Buffer buffer = default!;

        fixed (T* dataPtr = data)
        {
            SubresourceData initialData = new((nint)dataPtr);
            buffer = device.CreateBuffer(d3dDesc, initialData);
        }

        return new D3D11Buffer(buffer)
        {
            ByteSize = byteSize,
            ByteStride = byteStride
        };
    }

    public static D3D11Buffer Create(ID3D11Device5 device, in CoreGfx.BufferDescription desc)
    {
        BufferDescription d3dDesc = ToD3DBufferDescription(desc, false);

        bool isStructured = (d3dDesc.MiscFlags & ResourceOptionFlags.BufferStructured) == ResourceOptionFlags.BufferStructured;
        uint structureByteStride = isStructured ? desc.ByteStride : 0;

        d3dDesc.StructureByteStride = structureByteStride;

        ID3D11Buffer buffer = device.CreateBuffer(d3dDesc, initialData: null);

        return new D3D11Buffer(buffer)
        {
            ByteSize = d3dDesc.ByteWidth,
            ByteStride = desc.ByteStride
        };
    }

    private static BufferDescription ToD3DBufferDescription(in CoreGfx.BufferDescription desc, bool hasInitialData)
    {
        ResourceUsage resourceUsage;
        CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None;

        switch (desc.CpuAccess)
        {
            case CoreGfx.CpuAccess.None:
                // If we have an initial data, we can set Immutable for best performance
                resourceUsage = hasInitialData ? ResourceUsage.Immutable : ResourceUsage.Default;
                break;
            case CoreGfx.CpuAccess.Write:
                resourceUsage = ResourceUsage.Dynamic;
                cpuAccessFlags = CpuAccessFlags.Write;
                break;
            case CoreGfx.CpuAccess.Read:
                resourceUsage = ResourceUsage.Staging;
                cpuAccessFlags = CpuAccessFlags.Read;
                break;
            default:
                throw new NotSupportedException();
        }

        // If cpu access is read, then d3d resource usage is staging, for staging buffers BindFlags should be None
        BindFlags bindFlags = BindFlags.None;
        if (desc.CpuAccess != CoreGfx.CpuAccess.Read)
        {
            if ((desc.Usage & CoreGfx.BufferUsage.VertexBuffer) == CoreGfx.BufferUsage.VertexBuffer) bindFlags |= BindFlags.VertexBuffer;
            if ((desc.Usage & CoreGfx.BufferUsage.IndexBuffer) == CoreGfx.BufferUsage.IndexBuffer) bindFlags |= BindFlags.IndexBuffer;
            if ((desc.Usage & CoreGfx.BufferUsage.ConstantBuffer) == CoreGfx.BufferUsage.ConstantBuffer) bindFlags |= BindFlags.ConstantBuffer;
            if ((desc.Usage & CoreGfx.BufferUsage.ShaderResource) == CoreGfx.BufferUsage.ShaderResource ||
                (desc.Usage & CoreGfx.BufferUsage.StructuredBuffer) == CoreGfx.BufferUsage.StructuredBuffer) bindFlags |= BindFlags.ShaderResource;
            if ((desc.Usage & CoreGfx.BufferUsage.UnorderedAccess) == CoreGfx.BufferUsage.UnorderedAccess) bindFlags |= BindFlags.UnorderedAccess;
        }

        ResourceOptionFlags miscFlags = ResourceOptionFlags.None;
        if ((desc.Usage & CoreGfx.BufferUsage.IndirectArguments) == CoreGfx.BufferUsage.IndirectArguments)
        {
            miscFlags |= ResourceOptionFlags.DrawIndirectArguments;
        }

        bool isStructured = (desc.Usage & CoreGfx.BufferUsage.StructuredBuffer) == CoreGfx.BufferUsage.StructuredBuffer;
        if (isStructured)
        {
            miscFlags |= ResourceOptionFlags.BufferStructured;
        }

        if ((desc.Usage & CoreGfx.BufferUsage.RawBuffer) == CoreGfx.BufferUsage.RawBuffer)
        {
            miscFlags |= ResourceOptionFlags.BufferAllowRawViews;
        }

        return new BufferDescription
        {
            ByteWidth = desc.ByteSize,
            Usage = resourceUsage,
            BindFlags = bindFlags,
            CPUAccessFlags = cpuAccessFlags,
            MiscFlags = miscFlags,
            StructureByteStride = isStructured ? desc.ByteStride : 0
        };
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        Native.Dispose();

        _disposed = true;
    }
}
