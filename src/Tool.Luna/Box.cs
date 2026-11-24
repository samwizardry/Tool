using System.Numerics;

using Tool.Assets;
using Tool.Graphics;

namespace Tool.Luna;

public class Box : Actor
{
    public Box(GraphicsDevice device, AssetHandle<ModelAsset> model) : base(device, model)
    {
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

    public override void Draw(AssetManager assets)
    {
        base.Draw(assets);
    }
}
