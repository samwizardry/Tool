using System.Numerics;
using System.Runtime.InteropServices;

namespace Torture.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPositionNormal
{
    public static readonly VertexLayout Layout;

    public Vector3 Position;
    public Vector3 Normal;

    static VertexPositionNormal()
    {
        Layout = new VertexLayout([
            new VertexElement(VertexElementSemantic.Position, VertexElementFormat.Float3),
            new VertexElement(VertexElementSemantic.Normal, VertexElementFormat.Float3)
        ]);
    }

    public VertexPositionNormal(Vector3 position, Vector3 normal)
    {
        Position = position;
        Normal = normal;
    }
}
