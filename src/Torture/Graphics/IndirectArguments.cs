using System.Runtime.InteropServices;

namespace Torture.Graphics;

/// <summary>
/// Hardware-compatible indirect arguments for <see cref="GraphicsDevice.DrawIndirect"/> (non-indexed instanced draw).
/// Matches D3D11_DRAW_INSTANCED_INDIRECT_ARGS and VkDrawIndirectCommand.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct DrawIndirectArguments(
    uint VertexCountPerInstance,
    uint InstanceCount,
    uint StartVertexLocation = 0,
    uint StartInstanceLocation = 0);

/// <summary>
/// Hardware-compatible indirect arguments for <see cref="GraphicsDevice.DrawIndexedIndirect"/> (indexed instanced draw).
/// Matches D3D11_DRAW_INDEXED_INSTANCED_INDIRECT_ARGS and VkDrawIndexedIndirectCommand.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct DrawIndexedIndirectArguments(
    uint IndexCountPerInstance,
    uint InstanceCount,
    uint StartIndexLocation = 0,
    int BaseVertexLocation = 0,
    uint StartInstanceLocation = 0);

/// <summary>
/// Hardware-compatible indirect arguments for <see cref="GraphicsDevice.DispatchIndirect"/>.
/// Matches D3D11_DISPATCH_INDIRECT_ARGS and VkDispatchIndirectCommand.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct DispatchIndirectArguments(
    uint ThreadGroupCountX,
    uint ThreadGroupCountY = 1,
    uint ThreadGroupCountZ = 1);
