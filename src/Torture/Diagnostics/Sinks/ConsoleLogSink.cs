namespace Torture.Diagnostics;

/// <summary>Writes colored log lines to the process console.</summary>
public sealed class ConsoleLogSink : ILogSink
{
    public LogSeverity MinimumSeverity { get; set; } = LogSeverity.Trace;

    public void Write(in LogMessage message)
    {
        ConsoleColor previous = Console.ForegroundColor;
        Console.ForegroundColor = message.Severity switch
        {
            LogSeverity.Fatal => ConsoleColor.Magenta,
            LogSeverity.Error => ConsoleColor.Red,
            LogSeverity.Warning => ConsoleColor.Yellow,
            LogSeverity.Info => ConsoleColor.Gray,
            LogSeverity.Debug => ConsoleColor.DarkCyan,
            _ => ConsoleColor.DarkGray
        };
        LogFormatter.Write(Console.Out, message, includeDate: false, includeThread: false);
        Console.ForegroundColor = previous;
    }

    public void Flush() => Console.Out.Flush();

    public void Dispose() { }
}
