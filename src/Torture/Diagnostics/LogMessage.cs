namespace Torture.Diagnostics;

/// <summary>A single log entry passed to sinks.</summary>
/// <param name="Timestamp">UTC time of the message.</param>
/// <param name="Id">Source-specific message id (D3D11 MessageId, Vulkan messageIdNumber); 0 for engine messages.</param>
public readonly record struct LogMessage(
    DateTime Timestamp,
    LogSeverity Severity,
    LogCategory Category,
    string Text,
    int Id,
    int ThreadId,
    Exception? Exception);
