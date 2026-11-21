using System.Numerics;

using Torture.Collections;
using Torture.Graphics;

namespace Torture.Luna;

public class Box : Actor
{
    public Box(GraphicsDevice device, Material material)
        : base(device)
    {
        var data = Geometry.ComputeBox(Vector3.One * 2.0f);

        _model = new Model(new Mesh(
            device.CreateBuffer(data.Vertices.AsSpan(), BufferDescription.Vertex(0, 0)),
            device.CreateBuffer(data.Indices.AsSpan(), BufferDescription.Index(0))),
            material);

        _actorData.SetShininess(16.0f);
    }

    float _angle = 0.0f;

    public override void Update(float dt)
    {
        _angle += 30.0f * dt;
        _actorData.SetModelMatrix(Matrix4x4.CreateRotationY(float.DegreesToRadians(_angle)));

        if (Matrix4x4.Invert(_actorData.ModelMatrix, out var inverseModel))
        {
            inverseModel.Translation = Vector3.Zero;
            _actorData.SetNormalMatrix(Matrix4x4.Transpose(inverseModel));
        }
        else
        {
            _actorData.SetNormalMatrix(Matrix4x4.Identity);
        }

        base.Update(dt);
    }

    public override void Draw()
    {
        base.Draw();
    }
}
