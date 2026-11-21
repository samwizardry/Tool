using System.Runtime.CompilerServices;

namespace Torture.Graphics;

public readonly record struct BufferDescription
{
    public BufferDescription(
        uint byteSize,
        BufferUsage usage,
        CpuAccess cpuAccess = CpuAccess.None,
        uint byteStride = 0)
    {
        ByteSize = byteSize;
        Usage = usage;
        CpuAccess = cpuAccess;
        ByteStride = byteStride;
    }

    public BufferDescription(
        BufferUsage usage,
        bool isDynamic = false,
        uint byteSize = 0,
        uint byteStride = 0)
        : this(byteSize, usage, isDynamic ? CpuAccess.Write : CpuAccess.None, byteStride)
    {
    }

    public readonly uint ByteSize { get; init; }
    public readonly uint ByteStride { get; init; }
    public readonly BufferUsage Usage { get; init; }
    public readonly CpuAccess CpuAccess { get; init; }

    public static BufferDescription Vertex(uint byteSize, uint byteStride, bool isDynamic = false) =>
        new(byteSize, BufferUsage.VertexBuffer, isDynamic ? CpuAccess.Write : CpuAccess.None, byteStride);

    public static BufferDescription Index(uint byteSize, bool isDynamic = false) =>
        new(byteSize, BufferUsage.IndexBuffer, isDynamic ? CpuAccess.Write : CpuAccess.None);

    public static BufferDescription Constant(uint byteSize, bool isDynamic) =>
        new(Align16(byteSize), BufferUsage.ConstantBuffer, isDynamic ? CpuAccess.Write : CpuAccess.None);

    public static BufferDescription Constant<T>(bool isDynamic = true)
        where T : unmanaged =>
        Constant((uint)Unsafe.SizeOf<T>(), isDynamic);

    public static BufferDescription Structured<T>(uint elementCount, bool isDynamic = false, bool allowUav = false)
        where T : unmanaged =>
        new(
            elementCount * (uint)Unsafe.SizeOf<T>(),
            BufferUsage.StructuredBuffer | BufferUsage.ShaderResource | (allowUav ? BufferUsage.UnorderedAccess : BufferUsage.None),
            isDynamic ? CpuAccess.Write : CpuAccess.None,
            (uint)Unsafe.SizeOf<T>());

    public static BufferDescription IndirectArguments<T>(uint commandCount = 1u, bool allowUav = false)
        where T : unmanaged =>
        new(
            commandCount * (uint)Unsafe.SizeOf<T>(),
            BufferUsage.IndirectArguments | (allowUav ? BufferUsage.UnorderedAccess : BufferUsage.None),
            CpuAccess.None,
            (uint)Unsafe.SizeOf<T>());

    /// <summary>
    /// Aligns size up to the nearest multiple of 16 bytes (required by D3D11 constant buffers).
    /// </summary>
    private static uint Align16(uint size) => (size + 15u) & ~15u;
}

public abstract class Buffer : GraphicsResource, IBindableResource
{
    /// <summary>
    /// Buffer size in bytes.
    /// </summary>
    public uint ByteSize { get; protected set; }

    /// <summary>
    /// Buffer stride in bytes.
    /// </summary>
    public uint ByteStride { get; protected set; }

    /// <summary>
    /// Buffer element count.
    /// </summary>
    public uint Count => ByteSize / ByteStride;
}
