namespace Torture.Graphics;

/// <summary>
/// Runtime options for graphics device creation.
/// Defaults are controlled by the GRAPHICS_DEBUG build symbol and can be overridden from the command line.
/// </summary>
public sealed record GraphicsDeviceOptions
{
#if GRAPHICS_DEBUG
    private const bool DebugByDefault = true;
#else
    private const bool DebugByDefault = false;
#endif

    /// <summary>Enables the graphics API validation layer (D3D11 debug layer, Vulkan validation layers).</summary>
    public bool EnableValidation { get; init; } = DebugByDefault;
    /// <summary>
    /// Registers every native wrapper together with its creation stack trace to report leaks on shutdown.
    /// Allocates on every wrapper creation, so disable it when measuring managed allocations.
    /// </summary>
    public bool EnableObjectTracking { get; init; } = DebugByDefault;
    /// <summary>Breaks into the debugger on validation errors. Ignored when no debugger is attached.</summary>
    public bool BreakOnError { get; init; } = false;
}