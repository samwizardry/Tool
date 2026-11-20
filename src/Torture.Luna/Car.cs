using System.Numerics;

using Torture.Graphics;

namespace Torture.Luna;

public sealed class Car : Actor
{
    public Car(GraphicsDevice device, Material material) : base(device)
    {
        _model = ModelLoader.LoadLunaModel(device, material, "resources/objects/car.txt");

        _actorData.SetShininess(128.0f);
    }

    float _angle = 0.0f;

    public override void Update(float dt)
    {
        _angle += 30.0f * dt;
        _actorData.SetModelMatrix(
            Matrix4x4.CreateRotationY(float.DegreesToRadians(_angle)) *
            Matrix4x4.CreateTranslation(new Vector3(10.0f, 0.0f, 10.0f)));

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
