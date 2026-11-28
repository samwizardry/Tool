using Vortice.Mathematics;

namespace Tool.Graphics;

public readonly record struct NativeWindowInfo
{
    public readonly nint SdlWindow;
    public readonly nint WindowHandle;
    public readonly SizeI ClientSize;
    public readonly RectI Bounds;

    public NativeWindowInfo(
        nint sdlWindow,
        nint windowHandle,
        SizeI clientSize,
        RectI bounds)
    {
        SdlWindow = sdlWindow;
        WindowHandle = windowHandle;
        ClientSize = clientSize;
        Bounds = bounds;
    }
}
