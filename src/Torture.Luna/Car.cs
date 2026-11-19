using System.Numerics;

using Torture.Graphics;

namespace Torture.Luna;

public sealed class Car : Actor
{
    private readonly Model _model;

    public Car(GraphicsDevice device, Material material) : base(device)
    {
        _model = ModelLoader.LoadLunaModel(device, material, "resources/objects/car.txt");

        _perObjectData.Material = new()
        {
            Shininess = 32.0f
        };
    }

    public override void Update(float dt)
    {
        _perObjectData.Model = Matrix4x4.CreateTranslation(new Vector3(10.0f, 0.0f, 10.0f));
        base.Update(dt);
    }

    public override void Draw()
    {
        base.Draw();
        _model.Draw(_device, ResourceFactoryCache.PerFrameBindingSet, ResourceFactoryCache.PerObjBindingSet);
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _model?.Dispose();
        base.Dispose();

        _disposed = true;
    }
}
