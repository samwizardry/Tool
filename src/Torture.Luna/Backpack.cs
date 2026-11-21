using System.Numerics;

using Torture.Graphics;

namespace Torture.Luna;

public sealed class Backpack : Actor
{
    private readonly Material[] _materials;
    private readonly BindingSet[] _bindingSets;

    public Backpack(GraphicsDevice device) : base(device)
    {
        using var loader = new AssimpModelLoader(device);
        if (!loader.TryLoad("resources/objects/backpack/backpack.obj", out _model!, out _materials!, out _bindingSets!))
        {
            throw new InvalidOperationException();
        }

        _actorData.SetShininess(64.0f);
    }

    float _angle = 0.0f;

    public override void Update(float dt)
    {
        _angle += 30.0f * dt;
        _actorData.SetModelMatrix(
            Matrix4x4.CreateRotationY(float.DegreesToRadians(_angle)) *
            Matrix4x4.CreateTranslation(new Vector3(-10.0f, 0.0f, -10.0f)));

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
