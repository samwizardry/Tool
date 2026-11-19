using SharpGen.Runtime;

using Vortice.Direct3D11;

using TortGfx = Torture.Graphics;

namespace Torture.Direct3D11;

internal sealed class D3D11BindingSet : TortGfx.BindingSet
{
    internal readonly record struct BufferSlot(uint Slot, ID3D11Buffer Buffer);
    internal readonly record struct SrvSlot(uint Slot, ID3D11ShaderResourceView? Srv);
    internal readonly record struct UavSlot(uint Slot, ID3D11UnorderedAccessView? Uav);
    internal readonly record struct SamplerSlot(uint Slot, ID3D11SamplerState Sampler);

    private readonly TortGfx.ShaderType _visibility;
    private readonly BufferSlot[] _constantBuffers;
    private readonly SrvSlot[] _srvSlots;
    private readonly UavSlot[] _uavSlots;
    private readonly SamplerSlot[] _samplerSlots;

    public D3D11BindingSet(in TortGfx.BindingSetDescription desc, D3D11BindingLayout layout)
        : base(desc)
    {
        _visibility = layout.Visibility;

        List<BufferSlot> constantBuffers = [];
        List<SrvSlot> srvSlots = [];
        List<UavSlot> uavSlots = [];
        List<SamplerSlot> samplerSlots = [];

        foreach (ref readonly TortGfx.BindingSetItem item in desc.Bindings.AsSpan())
        {
            switch (item.Type)
            {
                case TortGfx.BindingResourceType.ConstantBuffer:
                    constantBuffers.Add(new(item.Slot, ((D3D11Buffer)item.Resource).Native));
                    break;

                case TortGfx.BindingResourceType.TextureSrv:
                    srvSlots.Add(new(item.Slot, ((D3D11Texture)item.Resource).Srv));
                    break;

                //case TortGfx.BindingResourceType.TextureUav:
                //    uavSlots.Add(new(item.Slot, ((D3D11Texture)item.Resource).Uav));
                //    break;

                case TortGfx.BindingResourceType.BufferSrv:
                    srvSlots.Add(new(item.Slot, ((D3D11Buffer)item.Resource).Srv));
                    break;

                case TortGfx.BindingResourceType.BufferUav:
                    uavSlots.Add(new(item.Slot, ((D3D11Buffer)item.Resource).Uav));
                    break;

                case TortGfx.BindingResourceType.Sampler:
                    samplerSlots.Add(new(item.Slot, ((D3D11Sampler)item.Resource).Native));
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        _constantBuffers = [.. constantBuffers];
        _srvSlots = [.. srvSlots];
        _uavSlots = [.. uavSlots];
        _samplerSlots = [.. samplerSlots];
    }

    public void Apply(ID3D11DeviceContext4 imContext)
    {
        bool vs = (_visibility & TortGfx.ShaderType.Vertex) == TortGfx.ShaderType.Vertex;
        bool hs = (_visibility & TortGfx.ShaderType.Hull) == TortGfx.ShaderType.Hull;
        bool ds = (_visibility & TortGfx.ShaderType.Domain) == TortGfx.ShaderType.Domain;
        bool gs = (_visibility & TortGfx.ShaderType.Geometry) == TortGfx.ShaderType.Geometry;
        bool ps = (_visibility & TortGfx.ShaderType.Pixel) == TortGfx.ShaderType.Pixel;
        bool cs = (_visibility & TortGfx.ShaderType.Compute) == TortGfx.ShaderType.Compute;

        foreach (ref readonly BufferSlot cb in _constantBuffers.AsSpan())
        {
            if (vs) imContext.VSSetConstantBuffer(cb.Slot, cb.Buffer);
            if (hs) imContext.HSSetConstantBuffer(cb.Slot, cb.Buffer);
            if (ds) imContext.DSSetConstantBuffer(cb.Slot, cb.Buffer);
            if (gs) imContext.GSSetConstantBuffer(cb.Slot, cb.Buffer);
            if (ps) imContext.PSSetConstantBuffer(cb.Slot, cb.Buffer);
            if (cs) imContext.CSSetConstantBuffer(cb.Slot, cb.Buffer);
        }

        foreach (ref readonly SrvSlot srv in _srvSlots.AsSpan())
        {
            if (vs) imContext.VSSetShaderResource(srv.Slot, srv.Srv);
            if (hs) imContext.HSSetShaderResource(srv.Slot, srv.Srv);
            if (ds) imContext.DSSetShaderResource(srv.Slot, srv.Srv);
            if (gs) imContext.GSSetShaderResource(srv.Slot, srv.Srv);
            if (ps) imContext.PSSetShaderResource(srv.Slot, srv.Srv!);
            if (cs) imContext.CSSetShaderResource(srv.Slot, srv.Srv);
        }

        foreach (ref readonly SamplerSlot sampler in _samplerSlots.AsSpan())
        {
            if (vs) imContext.VSSetSampler(sampler.Slot, sampler.Sampler);
            if (hs) imContext.HSSetSampler(sampler.Slot, sampler.Sampler);
            if (ds) imContext.DSSetSampler(sampler.Slot, sampler.Sampler);
            if (gs) imContext.GSSetSampler(sampler.Slot, sampler.Sampler);
            if (ps) imContext.PSSetSampler(sampler.Slot, sampler.Sampler);
            if (cs) imContext.CSSetSampler(sampler.Slot, sampler.Sampler);
        }

        if (cs)
        {
            foreach (ref readonly UavSlot uav in _uavSlots.AsSpan())
            {
                imContext.CSSetUnorderedAccessView(uav.Slot, uav.Uav);
            }
        }
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}
