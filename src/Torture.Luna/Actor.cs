using Torture.Graphics;

namespace Torture.Luna;

public abstract class Actor : IDisposable
{
    protected readonly GraphicsDevice _device;
    protected readonly BufferHandle _perObjectCb;
    protected readonly BindingSetHandle _perObjBindingSet;
    protected readonly PhongPerObjectData _actorData;

    protected Model _model = null!;

    protected Actor(GraphicsDevice device)
    {
        _device = device;

        _perObjectCb = device.CreateBuffer(BufferDescription.Constant<PhongObject>());
        _actorData = new PhongPerObjectData(_perObjectCb);

        _perObjBindingSet = device.CreateBindingSet(
            desc: new BindingSetDescription(
                Bindings: [
                    BindingSetItem.ConstantBuffer(1, _perObjectCb)
                ]),
            layout: ResourceFactoryCache.PhongPerObjBindingLayout);
    }

    public virtual void Update(float dt)
    {
        _actorData.Update(_device);
    }

    public virtual void Draw()
    {
        _model.Draw(_device, ResourceFactoryCache.PhongPerFrameBindingSet, _perObjBindingSet);
    }

    public virtual void Dispose()
    {
        _device.Destroy(_perObjBindingSet);
        _device.Destroy(_perObjectCb);
        _model?.Destroy(_device);
    }
}
