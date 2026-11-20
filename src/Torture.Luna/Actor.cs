using Torture.Graphics;

namespace Torture.Luna;

public abstract class Actor : IDisposable
{
    protected readonly GraphicsDevice _device;
    protected readonly Graphics.Buffer _perObjectCb;
    protected readonly BindingSet _perObjBindingSet;
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
                    new(1, BindingResourceType.ConstantBuffer, _perObjectCb)
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
        _perObjectCb.Dispose();
        _perObjBindingSet.Dispose();
        _model?.Dispose();
    }
}
