using Tool.Assets;
using Tool.Graphics;

namespace Tool.Luna;

public abstract class Actor : IDisposable
{
    protected readonly GraphicsDevice _device;
    protected readonly BufferHandle _perObjectCb;
    protected readonly BindingSetHandle _perObjBindingSet;
    protected readonly PhongPerObjectData _actorData;

    private readonly AssetHandle<ModelAsset> _model;

    protected Actor(GraphicsDevice device, AssetHandle<ModelAsset> model)
    {
        _device = device;
        _model = model;

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

    public virtual void Draw(AssetManager assets)
    {
        assets.DrawModel(_model, ResourceFactoryCache.PhongPerFrameBindingSet, _perObjBindingSet);
    }

    public virtual void Dispose()
    {
        _device.Destroy(_perObjBindingSet);
        _device.Destroy(_perObjectCb);
    }
}
