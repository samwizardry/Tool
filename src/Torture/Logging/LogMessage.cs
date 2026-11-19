namespace Torture.Logging;

public readonly record struct LogMessage(
    DateTime Timestamp,
    LogSeverity Severity,
    LogCategory Category,
    string Text,
    int Id,
    int ThreadId,
    Exception? Exception);
