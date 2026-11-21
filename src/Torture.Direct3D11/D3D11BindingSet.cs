using SharpGen.Runtime;

using Vortice.Direct3D11;

namespace Torture.Direct3D11;

internal sealed class D3D11BindingSet : CoreBindingSet
{
    internal readonly record struct BufferSlot(uint Slot, ID3D11Buffer Buffer);
    internal readonly record struct SrvSlot(uint Slot, ID3D11ShaderResourceView? Srv);
    internal readonly record struct UavSlot(uint Slot, ID3D11UnorderedAccessView? Uav);
    internal readonly record struct SamplerSlot(uint Slot, ID3D11SamplerState Sampler);

    private readonly CoreShaderType _visibility;
    private readonly BufferSlot[] _constantBuffers;
    private readonly SrvSlot[] _srvSlots;
    private readonly UavSlot[] _uavSlots;
    private readonly SamplerSlot[] _samplerSlots;

    public D3D11BindingSet(in CoreBindingSetDesc desc, D3D11BindingLayout layout)
        : base(desc)
    {
        _visibility = layout.Visibility;

        List<BufferSlot> constantBuffers = [];
        List<SrvSlot> srvSlots = [];
        List<UavSlot> uavSlots = [];
        List<SamplerSlot> samplerSlots = [];

        foreach (ref readonly CoreBindingSetItem item in desc.Bindings.AsSpan())
        {
            switch (item.Type)
            {
                case CoreBindingResourceType.ConstantBuffer:
                    constantBuffers.Add(new(item.Slot, ((D3D11Buffer)item.Resource).Native));
                    break;

                case CoreBindingResourceType.TextureSrv:
                    srvSlots.Add(new(item.Slot, ((D3D11Texture)item.Resource).Srv));
                    break;

                //case CoreBindingResourceType.TextureUav:
                //    uavSlots.Add(new(item.Slot, ((D3D11Texture)item.Resource).Uav));
                //    break;

                case CoreBindingResourceType.BufferSrv:
                    srvSlots.Add(new(item.Slot, ((D3D11Buffer)item.Resource).Srv));
                    break;

                case CoreBindingResourceType.BufferUav:
                    uavSlots.Add(new(item.Slot, ((D3D11Buffer)item.Resource).Uav));
                    break;

                case CoreBindingResourceType.Sampler:
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
        bool vs = (_visibility & CoreShaderType.Vertex) == CoreShaderType.Vertex;
        bool hs = (_visibility & CoreShaderType.Hull) == CoreShaderType.Hull;
        bool ds = (_visibility & CoreShaderType.Domain) == CoreShaderType.Domain;
        bool gs = (_visibility & CoreShaderType.Geometry) == CoreShaderType.Geometry;
        bool ps = (_visibility & CoreShaderType.Pixel) == CoreShaderType.Pixel;
        bool cs = (_visibility & CoreShaderType.Compute) == CoreShaderType.Compute;

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
