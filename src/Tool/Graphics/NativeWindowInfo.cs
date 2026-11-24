using Vortice.Mathematics;

namespace Tool.Graphics;

public readonly record struct NativeWindowInfo(
    nint SdlWindow,
    nint WindowHandle,
    SizeI ClientSize,
    RectI Bounds);
