using System.Numerics;
using System.Runtime.InteropServices;

namespace Torture.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPosition
{
    public static readonly VertexLayout Layout;

    public Vector3 Position;

    static VertexPosition()
    {
        Layout = new VertexLayout([
            new VertexElement(VertexElementSemantic.Position, VertexElementFormat.Float3)
        ]);
    }

    public VertexPosition(Vector3 position)
    {
        Position = position;
    }
}
