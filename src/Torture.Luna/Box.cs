using System.Numerics;

using Torture.Collections;
using Torture.Graphics;

namespace Torture.Luna;

public class Box : Actor
{
    private readonly Model _model;

    public Box(GraphicsDevice device, Material material)
        : base(device)
    {
        var data = Geometry.ComputeBox(Vector3.One * 2.0f);

        _model = new Model(new Mesh(
            device.CreateBuffer(data.Vertices.AsSpan(), BufferDescription.Vertex(0, 0)),
            device.CreateBuffer(data.Indices.AsSpan(), BufferDescription.Index(0))),
            material,
            false);

        _perObjectData.Material = new()
        {
            Shininess = 32.0f
        };
    }

    float _angle = 0.0f;

    public override void Update(float dt)
    {
        _angle += 30.0f * dt;
        _perObjectData.Model = Matrix4x4.CreateRotationY(float.DegreesToRadians(_angle));
        
        if (Matrix4x4.Invert(_perObjectData.Model, out var inverseModel))
        {
            inverseModel.Translation = Vector3.Zero;
            _perObjectData.NormalMatrix = Matrix4x4.Transpose(inverseModel);
        }
        else
        {
            _perObjectData.NormalMatrix = Matrix4x4.Identity;
        }
    }

    public override void Draw()
    {
        base.Draw();
        _model.Draw(_device, null, null);
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _model.Dispose();
        base.Dispose();

        _disposed = true;
    }
}
