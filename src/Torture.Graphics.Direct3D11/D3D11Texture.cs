using Vortice.Direct3D11;

using CoreGfx = Torture.Core.Graphics;

namespace Torture.Graphics.Direct3D11;

internal sealed class D3D11Texture : CoreGfx.Texture
{
    public D3D11Texture(ID3D11Resource texture, ID3D11ShaderResourceView? srv)
    {
        Native = texture;
        Srv = srv;
    }

    public ID3D11Resource Native { get; }

    public ID3D11ShaderResourceView? Srv { get; }

    public static unsafe D3D11Texture Create<T>(ID3D11Device5 device, ReadOnlySpan<T> data, in CoreGfx.TextureDescription desc)
        where T : unmanaged
    {
        ID3D11Resource texture = default!;
        ID3D11ShaderResourceView? srv = null;

        fixed (T* dataPtr = data)
        {
            SubresourceData initialData = new((nint)dataPtr, rowPitch: desc.Width * 4u, slicePitch: 0);
            switch (desc.Dimension)
            {
                case CoreGfx.TextureDimension.Unknown:
                    break;
                case CoreGfx.TextureDimension.Texture1D:
                    break;
                case CoreGfx.TextureDimension.Texture1DArray:
                    break;

                case CoreGfx.TextureDimension.Texture2D:
                    var tex2d = device.CreateTexture2D(ToD3DTexture2DDescription(desc, true), initialData);
                    texture = tex2d;

                    if ((desc.Usage & CoreGfx.TextureUsage.ShaderResource) == CoreGfx.TextureUsage.ShaderResource)
                    {
                        srv = device.CreateShaderResourceView(
                            tex2d,
                            new ShaderResourceViewDescription(
                                tex2d,
                                Vortice.Direct3D.ShaderResourceViewDimension.Texture2D));
                    }

                    break;

                case CoreGfx.TextureDimension.Texture2DArray:
                    break;
                case CoreGfx.TextureDimension.TextureCube:
                    break;
                case CoreGfx.TextureDimension.TextureCubeArray:
                    break;
                case CoreGfx.TextureDimension.Texture2DMS:
                    break;
                case CoreGfx.TextureDimension.Texture2DMSArray:
                    break;
                case CoreGfx.TextureDimension.Texture3D:
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        return new D3D11Texture(texture, srv);
    }

    public static D3D11Texture Create(ID3D11Device5 device, in CoreGfx.TextureDescription desc)
    {
        ID3D11Resource texture = default!;
        ID3D11ShaderResourceView? srv = null;

        switch (desc.Dimension)
        {
            case CoreGfx.TextureDimension.Unknown:
                break;
            case CoreGfx.TextureDimension.Texture1D:
                break;
            case CoreGfx.TextureDimension.Texture1DArray:
                break;

            case CoreGfx.TextureDimension.Texture2D:
                var tex2d = device.CreateTexture2D(ToD3DTexture2DDescription(desc, true));
                texture = tex2d;

                if ((desc.Usage & CoreGfx.TextureUsage.ShaderResource) == CoreGfx.TextureUsage.ShaderResource)
                {
                    srv = device.CreateShaderResourceView(
                        tex2d,
                        new ShaderResourceViewDescription(
                            tex2d,
                            Vortice.Direct3D.ShaderResourceViewDimension.Texture2D));
                }

                break;

            case CoreGfx.TextureDimension.Texture2DArray:
                break;
            case CoreGfx.TextureDimension.TextureCube:
                break;
            case CoreGfx.TextureDimension.TextureCubeArray:
                break;
            case CoreGfx.TextureDimension.Texture2DMS:
                break;
            case CoreGfx.TextureDimension.Texture2DMSArray:
                break;
            case CoreGfx.TextureDimension.Texture3D:
                break;
            default:
                throw new NotSupportedException();
        }

        return new D3D11Texture(texture, srv);
    }

    private static Texture2DDescription ToD3DTexture2DDescription(in CoreGfx.TextureDescription desc, bool hasInitialData)
    {
        BindFlags bindFlags = BindFlags.None;
        if ((desc.Usage & CoreGfx.TextureUsage.ShaderResource) == CoreGfx.TextureUsage.ShaderResource)
        {
            bindFlags |= BindFlags.ShaderResource;
        }
        if ((desc.Usage & CoreGfx.TextureUsage.RenderTarget) == CoreGfx.TextureUsage.RenderTarget)
        {
            bindFlags |= BindFlags.RenderTarget;
        }
        if ((desc.Usage & CoreGfx.TextureUsage.DepthStencil) == CoreGfx.TextureUsage.DepthStencil)
        {
            bindFlags |= BindFlags.DepthStencil;
        }
        if ((desc.Usage & CoreGfx.TextureUsage.UnorderedAccess) == CoreGfx.TextureUsage.UnorderedAccess)
        {
            bindFlags |= BindFlags.UnorderedAccess;
        }

        ResourceUsage usage = ResourceUsage.Default;
        CpuAccessFlags cpuAccessFlags;
        switch (desc.CpuAccess)
        {
            case CoreGfx.CpuAccess.None:
                usage |= hasInitialData ? ResourceUsage.Immutable : ResourceUsage.Default;
                cpuAccessFlags = CpuAccessFlags.None;
                break;
            case CoreGfx.CpuAccess.Write:
                usage |= ResourceUsage.Dynamic;
                cpuAccessFlags = CpuAccessFlags.Write;
                break;
            case CoreGfx.CpuAccess.Read:
                usage |= ResourceUsage.Staging;
                cpuAccessFlags = CpuAccessFlags.Read;
                break;
            default:
                throw new NotSupportedException();
        }

        return new Texture2DDescription(desc.Format.ToDXGIFormat(),
            desc.Width, desc.Height,
            arraySize: desc.ArraySize, mipLevels: desc.MipLevels,
            bindFlags: bindFlags, usage: usage, cpuAccessFlags: cpuAccessFlags,
            sampleCount: desc.SampleCount, sampleQuality: desc.SampleQuality,
            miscFlags: ResourceOptionFlags.None);
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        Srv?.Dispose();
        Native?.Dispose();

        _disposed = true;
    }
}
