// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Tool.Direct3D11;

internal readonly struct D3D11Texture
{
    public readonly Graphics.TextureDescription Desc;
    public readonly ID3D11Resource Native;
    public readonly ID3D11ShaderResourceView? Srv;

    private D3D11Texture(in Graphics.TextureDescription desc, ID3D11Resource texture, ID3D11ShaderResourceView? srv)
    {
        Desc = desc;
        Native = texture;
        Srv = srv;
    }

    public void Destroy()
    {
        Srv?.Dispose();
        Native?.Dispose();
    }

    public static unsafe D3D11Texture Create<T>(ID3D11Device5 device,
        ID3D11DeviceContext4 imContext,
        ReadOnlySpan<T> data,
        in Graphics.TextureDescription desc)
        where T : unmanaged
    {
        fixed (T* dataPtr = data)
        {
            Graphics.FormatInfo.GetSurfaceInfo(desc.Format, desc.Width, desc.Height, out uint rowPitch, out uint slicePitch);

            uint expectedBytes = slicePitch * desc.Depth;
            uint actualBytes = (uint)(sizeof(T) * data.Length);
            if (actualBytes < expectedBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(data),
                    $"Provided data size ({actualBytes} bytes) is smaller than the required surface size ({expectedBytes} bytes).");
            }

            if (desc.MipLevels == 1 && desc.ArraySize == 1)
            {
                SubresourceData initialData = new((nint)dataPtr, rowPitch, slicePitch);
                return Create(device, in desc, &initialData, generateMips: false);
            }
            else
            {
                var texture = Create(device, in desc, null, true);

                imContext.UpdateSubresource(texture.Native, 0, null, (nint)dataPtr, rowPitch, slicePitch);
                imContext.GenerateMips(texture.Srv);

                return texture;
            }
        }
    }

    public static unsafe D3D11Texture Create(ID3D11Device5 device, in Graphics.TextureDescription desc)
    {
        return Create(device, in desc, null, false);
    }

    private static unsafe D3D11Texture Create(ID3D11Device5 device,
        in Graphics.TextureDescription desc,
        SubresourceData* initialData,
        bool generateMips)
    {
        ref readonly var formatMapping = ref desc.Format.GetDxgiFormatMapping();
        bool isDepthStencil = (desc.Usage & Graphics.TextureUsage.DepthStencil) != 0;
        var format = isDepthStencil ? formatMapping.ResourceFormat : formatMapping.SrvFormat;

        BindFlags bindFlags = GetBindFlags(desc.Usage);
        if (generateMips)
        {
            bindFlags |= BindFlags.ShaderResource | BindFlags.RenderTarget;
        }

        GetResourceUsageAndCpuAccess(desc, initialData != null && !generateMips, out ResourceUsage usage, out CpuAccessFlags cpuAccess);

        ResourceOptionFlags miscFlags = GetMiscFlags(desc.Dimension);
        if (generateMips)
        {
            miscFlags |= ResourceOptionFlags.GenerateMips;
        }

        ID3D11Resource texture;
        switch (desc.Dimension)
        {
            case Graphics.TextureDimension.Texture1D:
            case Graphics.TextureDimension.Texture1DArray:
                {
                    Texture1DDescription nativeDesc = new(
                        format: format,
                        width: desc.Width,
                        arraySize: desc.ArraySize,
                        mipLevels: desc.MipLevels,
                        bindFlags: bindFlags,
                        usage: usage,
                        cpuAccessFlags: cpuAccess,
                        miscFlags: miscFlags);
                    texture = initialData != null
                        ? device.CreateTexture1D(nativeDesc, new Span<SubresourceData>(initialData, 1))
                        : device.CreateTexture1D(nativeDesc);
                    break;
                }
            case Graphics.TextureDimension.Texture2D:
            case Graphics.TextureDimension.Texture2DArray:
            case Graphics.TextureDimension.TextureCube:
            case Graphics.TextureDimension.TextureCubeArray:
            case Graphics.TextureDimension.Texture2DMS:
            case Graphics.TextureDimension.Texture2DMSArray:
                {
                    Texture2DDescription nativeDesc = new(
                        format: format,
                        width: desc.Width,
                        height: desc.Height,
                        arraySize: desc.ArraySize,
                        mipLevels: desc.MipLevels,
                        bindFlags: bindFlags,
                        usage: usage,
                        cpuAccessFlags: cpuAccess,
                        sampleCount: desc.SampleCount,
                        sampleQuality: desc.SampleQuality,
                        miscFlags: miscFlags);
                    texture = initialData != null
                        ? device.CreateTexture2D(nativeDesc, new Span<SubresourceData>(initialData, 1))
                        : device.CreateTexture2D(nativeDesc);
                    break;
                }
            case Graphics.TextureDimension.Texture3D:
                {
                    Texture3DDescription nativeDesc = new(
                        format: format,
                        width: desc.Width,
                        height: desc.Height,
                        depth: desc.Depth,
                        mipLevels: desc.MipLevels,
                        bindFlags: bindFlags,
                        usage: usage,
                        cpuAccessFlags: cpuAccess,
                        miscFlags: miscFlags);
                    texture = initialData != null
                        ? device.CreateTexture3D(nativeDesc, new Span<SubresourceData>(initialData, 1))
                        : device.CreateTexture3D(nativeDesc);
                    break;
                }
            default:
                throw new NotSupportedException();
        }

        ID3D11ShaderResourceView? srv = null;
        if ((desc.Usage & Graphics.TextureUsage.ShaderResource) != 0 || generateMips)
        {
            srv = CreateSrv(device, texture, in desc, formatMapping.SrvFormat);
        }

        return new D3D11Texture(in desc, texture, srv);
    }

    private static ID3D11ShaderResourceView? CreateSrv(ID3D11Device5 device, ID3D11Resource texture, in Graphics.TextureDescription desc, Format srvFormat)
    {
        ShaderResourceViewDescription srvDesc = new() { Format = srvFormat };

        switch (desc.Dimension)
        {
            case Graphics.TextureDimension.Texture1D:
                srvDesc.ViewDimension = ShaderResourceViewDimension.Texture1D;
                srvDesc.Texture1D = new Texture1DShaderResourceView { MostDetailedMip = 0, MipLevels = desc.MipLevels };
                break;
            case Graphics.TextureDimension.Texture1DArray:
                srvDesc.ViewDimension = ShaderResourceViewDimension.Texture1DArray;
                srvDesc.Texture1DArray = new Texture1DArrayShaderResourceView
                {
                    MostDetailedMip = 0,
                    MipLevels = desc.MipLevels,
                    FirstArraySlice = 0,
                    ArraySize = desc.ArraySize
                };
                break;
            case Graphics.TextureDimension.Texture2D:
                srvDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
                srvDesc.Texture2D = new Texture2DShaderResourceView { MostDetailedMip = 0, MipLevels = desc.MipLevels };
                break;
            case Graphics.TextureDimension.Texture2DArray:
                srvDesc.ViewDimension = ShaderResourceViewDimension.Texture2DArray;
                srvDesc.Texture2DArray = new Texture2DArrayShaderResourceView
                {
                    MostDetailedMip = 0,
                    MipLevels = desc.MipLevels,
                    FirstArraySlice = 0,
                    ArraySize = desc.ArraySize
                };
                break;
            case Graphics.TextureDimension.TextureCube:
                srvDesc.ViewDimension = ShaderResourceViewDimension.TextureCube;
                srvDesc.TextureCube = new TextureCubeShaderResourceView { MostDetailedMip = 0, MipLevels = desc.MipLevels };
                break;
            case Graphics.TextureDimension.TextureCubeArray:
                srvDesc.ViewDimension = ShaderResourceViewDimension.TextureCubeArray;
                srvDesc.TextureCubeArray = new TextureCubeArrayShaderResourceView
                {
                    MostDetailedMip = 0,
                    MipLevels = desc.MipLevels,
                    First2DArrayFace = 0,
                    NumCubes = desc.ArraySize / 6
                };
                break;
            case Graphics.TextureDimension.Texture2DMS:
                srvDesc.ViewDimension = ShaderResourceViewDimension.Texture2DMultisampled;
                break;
            case Graphics.TextureDimension.Texture2DMSArray:
                srvDesc.ViewDimension = ShaderResourceViewDimension.Texture2DMultisampledArray;
                srvDesc.Texture2DMSArray = new Texture2DMultisampledArrayShaderResourceView
                {
                    FirstArraySlice = 0,
                    ArraySize = desc.ArraySize
                };
                break;
            case Graphics.TextureDimension.Texture3D:
                srvDesc.ViewDimension = ShaderResourceViewDimension.Texture3D;
                srvDesc.Texture3D = new Texture3DShaderResourceView { MostDetailedMip = 0, MipLevels = desc.MipLevels };
                break;
            default:
                throw new NotSupportedException();
        }

        return device.CreateShaderResourceView(texture, srvDesc);
    }

    private static BindFlags GetBindFlags(Graphics.TextureUsage usage)
    {
        BindFlags flags = BindFlags.None;
        if ((usage & Graphics.TextureUsage.ShaderResource) == Graphics.TextureUsage.ShaderResource)
            flags |= BindFlags.ShaderResource;
        if ((usage & Graphics.TextureUsage.RenderTarget) == Graphics.TextureUsage.RenderTarget)
            flags |= BindFlags.RenderTarget;
        if ((usage & Graphics.TextureUsage.DepthStencil) == Graphics.TextureUsage.DepthStencil)
            flags |= BindFlags.DepthStencil;
        if ((usage & Graphics.TextureUsage.UnorderedAccess) == Graphics.TextureUsage.UnorderedAccess)
            flags |= BindFlags.UnorderedAccess;
        return flags;
    }

    private static void GetResourceUsageAndCpuAccess(
        in Graphics.TextureDescription desc,
        bool hasInitialData,
        out ResourceUsage usage,
        out CpuAccessFlags cpuAccessFlags)
    {
        bool isGpuWritable = (desc.Usage & (Graphics.TextureUsage.RenderTarget | Graphics.TextureUsage.DepthStencil | Graphics.TextureUsage.UnorderedAccess)) != 0;
        switch (desc.CpuAccess)
        {
            case Graphics.CpuAccess.None:
                usage = (hasInitialData && !isGpuWritable && desc.MipLevels == 1 && desc.ArraySize == 1)
                    ? ResourceUsage.Immutable
                    : ResourceUsage.Default;
                cpuAccessFlags = CpuAccessFlags.None;
                break;
            case Graphics.CpuAccess.Write:
                usage = ResourceUsage.Dynamic;
                cpuAccessFlags = CpuAccessFlags.Write;
                break;
            case Graphics.CpuAccess.Read:
                usage = ResourceUsage.Staging;
                cpuAccessFlags = CpuAccessFlags.Read;
                break;
            default:
                throw new NotSupportedException();
        }
    }

    private static ResourceOptionFlags GetMiscFlags(Graphics.TextureDimension dimension) =>
        dimension is Graphics.TextureDimension.TextureCube or Graphics.TextureDimension.TextureCubeArray
            ? ResourceOptionFlags.TextureCube
            : ResourceOptionFlags.None;
}
