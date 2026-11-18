namespace Torture.Diagnostics;

/// <summary>Message severity. Superset of NVRHI, D3D11 and Vulkan severities.</summary>
public enum LogSeverity : byte
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    Fatal,
    /// <summary>Filter-only value: disables all messages.</summary>
    None
}