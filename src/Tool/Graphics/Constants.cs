// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

namespace Tool.Graphics;

public static class Constants
{
    public const int MaxRenderTargets = 8;
    public const int MaxViewports = 16;
    public const int MaxVertexAttributes = 16;
    public const int MaxBindingLayouts = 8;
    public const int MaxBindlessRegisterSpaces = 16;
    public const int MaxVolatileConstantBuffersPerLayout = 6;
    public const int MaxVolatileConstantBuffers = 32;
    public const int MaxPushConstantSize = 128; // D3D12: root signature is 256 bytes max., Vulkan: 128 bytes of push constants guaranteed
    public const int ConstantBufferOffsetSizeAlignment = 256; // Partially bound constant buffers must have offsets aligned to this and sizes multiple of this
}
