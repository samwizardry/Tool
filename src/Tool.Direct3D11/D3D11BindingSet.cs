using Tool.Collections;
using Tool.Graphics;

using Vortice.Direct3D11;

namespace Tool.Direct3D11;

internal readonly struct D3D11BindingSet
{
    internal readonly record struct BufferSlot
    {
        public readonly uint Slot;
        public readonly ID3D11Buffer Buffer;

        public BufferSlot(uint slot, ID3D11Buffer buffer)
        {
            Slot = slot;
            Buffer = buffer;
        }
    }

    internal readonly record struct SrvSlot
    {
        public readonly uint Slot;
        public readonly ID3D11ShaderResourceView? Srv;

        public SrvSlot(uint slot, ID3D11ShaderResourceView? srv)
        {
            Slot = slot;
            Srv = srv;
        }
    }

    internal readonly record struct UavSlot
    {
        public readonly uint Slot;
        public readonly ID3D11UnorderedAccessView? Uav;

        public UavSlot(uint slot, ID3D11UnorderedAccessView? uav)
        {
            Slot = slot;
            Uav = uav;
        }
    }

    internal readonly record struct SamplerSlot
    {
        public readonly uint Slot;
        public readonly ID3D11SamplerState Sampler;

        public SamplerSlot(uint slot, ID3D11SamplerState sampler)
        {
            Slot = slot;
            Sampler = sampler;
        }
    }

#if GRAPHICS_DEBUG
    public readonly BindingLayoutHandle Layout;
#endif
    private readonly ShaderType _visibility;
    private readonly BufferSlot[] _constantBuffers;
    private readonly SrvSlot[] _srvSlots;
    private readonly UavSlot[] _uavSlots;
    private readonly SamplerSlot[] _samplerSlots;

    private D3D11BindingSet(
#if GRAPHICS_DEBUG
        BindingLayoutHandle layout,
#endif
        ShaderType visibility,
        BufferSlot[] constantBuffers,
        SrvSlot[] srvSlots,
        UavSlot[] uavSlots,
        SamplerSlot[] samplerSlots)
    {
#if GRAPHICS_DEBUG
        Layout = layout;
#endif
        _visibility = visibility;
        _constantBuffers = constantBuffers;
        _srvSlots = srvSlots;
        _uavSlots = uavSlots;
        _samplerSlots = samplerSlots;
    }

#pragma warning disable CA1822 // Пометьте члены как статические
    public void Destroy()
#pragma warning restore CA1822 // Пометьте члены как статические
    {
    }

    public static D3D11BindingSet Create(
#if GRAPHICS_DEBUG
        BindingLayoutHandle layout,
#endif
        BindingSetDescription desc,
        D3D11BindingLayout layoutData,
        SlotMap<D3D11Buffer> buffers,
        SlotMap<D3D11Texture> textures,
        SlotMap<D3D11Sampler> samplers)
    {
        int constBufferCount = 0;
        int srvCount = 0;
        int uavCount = 0;
        int samplerCount = 0;

        for (int i = 0; i < desc.Bindings.Length; i++)
        {
            var item = desc.Bindings[i];
            switch (item.Type)
            {
                case BindingResourceType.ConstantBuffer:
                    constBufferCount++;
                    break;
                case BindingResourceType.TextureSrv:
                    srvCount++;
                    break;
                case BindingResourceType.BufferSrv:
                    srvCount++;
                    break;
                case BindingResourceType.BufferUav:
                    uavCount++;
                    break;
                case BindingResourceType.Sampler:
                    samplerCount++;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        BufferSlot[] constantBuffers = new BufferSlot[constBufferCount];
        SrvSlot[] srvSlots = new SrvSlot[srvCount];
        UavSlot[] uavSlots = new UavSlot[uavCount];
        SamplerSlot[] samplerSlots = new SamplerSlot[samplerCount];

        constBufferCount = 0;
        srvCount = 0;
        uavCount = 0;
        samplerCount = 0;

        foreach (BindingSetItem item in desc.Bindings)
        {
            switch (item.Type)
            {
                case BindingResourceType.ConstantBuffer:
                    constantBuffers[constBufferCount++] = new(item.Slot, buffers.Get(item.Resource).Native);
                    break;
                case BindingResourceType.TextureSrv:
                    srvSlots[srvCount++] = new(item.Slot, textures.Get(item.Resource).Srv);
                    break;
                case BindingResourceType.BufferSrv:
                    srvSlots[srvCount++] = new(item.Slot, buffers.Get(item.Resource).Srv);
                    break;
                case BindingResourceType.BufferUav:
                    uavSlots[uavCount++] = new(item.Slot, buffers.Get(item.Resource).Uav);
                    break;
                case BindingResourceType.Sampler:
                    samplerSlots[samplerCount++] = new(item.Slot, samplers.Get(item.Resource).Native);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        return new D3D11BindingSet(
#if GRAPHICS_DEBUG
            layout,
#endif
            layoutData.Visibility,
            constantBuffers,
            srvSlots,
            uavSlots,
            samplerSlots);
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
