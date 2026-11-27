using Tool.Assets;
using Tool.Graphics;

namespace Tool.Luna;

public abstract class Actor : IDisposable
{
    protected readonly GraphicsDevice _device;
    protected readonly BufferHandle _perObjectCB;
    protected readonly BindingSetHandle _perObjBS;
    protected readonly Phong.PerObjDCB _perObj;

    private readonly AssetHandle<ModelAsset> _model;

    protected Actor(GraphicsDevice device, AssetHandle<ModelAsset> model)
    {
        _device = device;
        _model = model;

        _perObjectCB = device.CreateBuffer(BufferDescription.Constant<Phong.PerObjData>());
        _perObj = new Phong.PerObjDCB(_perObjectCB);

        _perObjBS = device.CreateBindingSet(
            desc: new BindingSetDescription(
                bindings: [
                    BindingSetItem.ConstantBuffer(3, _perObjectCB)
                ]),
            layout: Phong.PerObjBL);
    }

    public virtual void Update(float dt)
    {
        _perObj.Update(_device);
    }

    public virtual void Draw(AssetManager assets)
    {
        assets.DrawModel(_model, Resources.Viewport, Phong.PerFrameBS, _perObjBS);
    }

    public virtual void Dispose()
    {
        _device.Destroy(_perObjBS);
        _device.Destroy(_perObjectCB);
    }
}
