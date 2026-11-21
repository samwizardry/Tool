// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

namespace Torture.Graphics;

public readonly record struct RasterStateDescription(
    RasterFillMode FillMode = RasterFillMode.Solid,
    RasterCullMode CullMode = RasterCullMode.Back,
    bool FrontCounterClockwise = true,
    bool DepthClipEnable = true,
    bool ScissorEnable = false,
    bool MultisampleEnable = false,
    bool AntialiasedLineEnable = false,
    int DepthBias = 0,
    float DepthBiasClamp = 0.0f,
    float SlopeScaledDepthBias = 0.0f,
    byte ForcedSampleCount = 0,
    bool ConservativeRasterEnable = false)
{
    public static RasterStateDescription CullBack => new(FillMode: RasterFillMode.Solid, CullMode: RasterCullMode.Back);
    public static RasterStateDescription CullFront => new(FillMode: RasterFillMode.Solid, CullMode: RasterCullMode.Front);
    public static RasterStateDescription CullNone => new(FillMode: RasterFillMode.Solid, CullMode: RasterCullMode.None);
    public static RasterStateDescription Wireframe => new(FillMode: RasterFillMode.Wireframe, CullMode: RasterCullMode.None);
}

public abstract class RasterState : GraphicsResource
{
    protected readonly RasterStateDescription _desc;

    protected RasterState(in RasterStateDescription desc)
    {
        _desc = desc;
    }

    public ref readonly RasterStateDescription Desc => ref _desc;
}
