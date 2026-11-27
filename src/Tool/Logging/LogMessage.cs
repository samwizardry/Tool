namespace Tool.Logging;

public readonly record struct LogMessage
{
    public readonly DateTime Timestamp;
    public readonly LogSeverity Severity;
    public readonly LogCategory Category;
    public readonly string Text;
    public readonly int Id;
    public readonly int ThreadId;
    public readonly Exception? Exception;

    public LogMessage(
        DateTime timestamp,
        LogSeverity severity,
        LogCategory category,
        string text,
        int id,
        int threadId,
        Exception? exception)
    {
        Timestamp = timestamp;
        Severity = severity;
        Category = category;
        Text = text;
        Id = id;
        ThreadId = threadId;
        Exception = exception;
    }
}
