using System.Numerics;
using System.Runtime.InteropServices;

namespace Tool.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertexPosition
{
    public static readonly VertexLayout Layout = new VertexLayout(
        VertexElement.Position);

    public Vector3 Position;

    public VertexPosition(Vector3 position)
    {
        Position = position;
    }
}
