using System.Numerics;

using Tool.Assets;
using Tool.Graphics;

namespace Tool.Luna;

public class Box : Actor
{
    public Box(GraphicsDevice device, AssetHandle<ModelAsset> model)
        : base(device, model)
    {
    }

    float _angle;

    public override void Update(float dt)
    {
        _angle += 30.0f * dt;

        _perObj.SetModelMatrix(
            Matrix4x4.CreateRotationY(float.DegreesToRadians(_angle)) *
            Matrix4x4.CreateTranslation(new Vector3(0.0f, 1.0f, 0.0f)));

        if (Matrix4x4.Invert(_perObj.ModelMatrix, out var inverseModel))
        {
            inverseModel.Translation = Vector3.Zero;
            _perObj.SetNormalMatrix(Matrix4x4.Transpose(inverseModel));
        }
        else
        {
            _perObj.SetNormalMatrix(Matrix4x4.Identity);
        }

        base.Update(dt);
    }

    public override void Draw(AssetManager assets)
    {
        base.Draw(assets);
    }
}
