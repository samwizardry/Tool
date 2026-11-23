using Torture.Collections;
using Torture.Graphics;

using Vortice.Direct3D11;

namespace Torture.Direct3D11;

internal readonly struct D3D11BindingSet
{
    internal readonly record struct BufferSlot(uint Slot, ID3D11Buffer Buffer);
    internal readonly record struct SrvSlot(uint Slot, ID3D11ShaderResourceView? Srv);
    internal readonly record struct UavSlot(uint Slot, ID3D11UnorderedAccessView? Uav);
    internal readonly record struct SamplerSlot(uint Slot, ID3D11SamplerState Sampler);

    private readonly ShaderType _visibility;
    private readonly BufferSlot[] _constantBuffers;
    private readonly SrvSlot[] _srvSlots;
    private readonly UavSlot[] _uavSlots;
    private readonly SamplerSlot[] _samplerSlots;

    public D3D11BindingSet(ShaderType visibility,
        BufferSlot[] constantBuffers,
        SrvSlot[] srvSlots,
        UavSlot[] uavSlots,
        SamplerSlot[] samplerSlots)
    {
        _visibility = visibility;
        _constantBuffers = constantBuffers;
        _srvSlots = srvSlots;
        _uavSlots = uavSlots;
        _samplerSlots = samplerSlots;
    }

    public void Destroy()
    {
    }

    public static D3D11BindingSet Create(
        in BindingSetDescription desc,
        in D3D11BindingLayout layout,
        SlotMap<D3D11Buffer> buffers,
        SlotMap<D3D11Texture> textures,
        SlotMap<D3D11Sampler> samplers)
    {
        List<BufferSlot> constantBuffers = [];
        List<SrvSlot> srvSlots = [];
        List<UavSlot> uavSlots = [];
        List<SamplerSlot> samplerSlots = [];

        foreach (ref readonly BindingSetItem item in desc.Bindings.AsSpan())
        {
            switch (item.Type)
            {
                case BindingResourceType.ConstantBuffer:
                    constantBuffers.Add(new(item.Slot, buffers.Get(item.Resource).Native));
                    break;
                case BindingResourceType.TextureSrv:
                    srvSlots.Add(new(item.Slot, textures.Get(item.Resource).Srv));
                    break;
                case BindingResourceType.BufferSrv:
                    srvSlots.Add(new(item.Slot, buffers.Get(item.Resource).Srv));
                    break;
                case BindingResourceType.BufferUav:
                    uavSlots.Add(new(item.Slot, buffers.Get(item.Resource).Uav));
                    break;
                case BindingResourceType.Sampler:
                    samplerSlots.Add(new(item.Slot, samplers.Get(item.Resource).Native));
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        return new D3D11BindingSet(
            layout.Visibility,
            [.. constantBuffers],
            [.. srvSlots],
            [.. uavSlots],
            [.. samplerSlots]);
    }

    public void Apply(ID3D11DeviceContext4 imContext)
    {
        bool vs = (_visibility & ShaderType.Vertex) == ShaderType.Vertex;
        bool hs = (_visibility & ShaderType.Hull) == ShaderType.Hull;
        bool ds = (_visibility & ShaderType.Domain) == ShaderType.Domain;
        bool gs = (_visibility & ShaderType.Geometry) == ShaderType.Geometry;
        bool ps = (_visibility & ShaderType.Pixel) == ShaderType.Pixel;
        bool cs = (_visibility & ShaderType.Compute) == ShaderType.Compute;

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
}
