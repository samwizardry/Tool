using System.Numerics;
using System.Runtime.InteropServices;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionNormal
{
    public static readonly VertexLayout Layout = new VertexLayout(
        VertexElement.Position,
        VertexElement.Normal);

    public Vector3 Position;
    public Vector3 Normal;

    public VertexPositionNormal(
        Vector3 position,
        Vector3 normal)
    {
        Position = position;
        Normal = normal;
    }
}
