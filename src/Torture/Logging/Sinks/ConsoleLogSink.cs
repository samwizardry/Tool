namespace Torture.Logging.Sinks;

public sealed class ConsoleLogSink : ILogSink
{
    public LogSeverity MinimumSeverity => LogSeverity.Message;

    public void Write(in LogMessage message)
    {
        ConsoleColor previous = Console.ForegroundColor;

        Console.ForegroundColor = message.Severity switch
        {
            LogSeverity.Fatal => ConsoleColor.Magenta,
            LogSeverity.Error => ConsoleColor.Red,
            LogSeverity.Warning => ConsoleColor.Yellow,
            LogSeverity.Info => ConsoleColor.Gray,
            LogSeverity.Message => ConsoleColor.DarkCyan,
            _ => ConsoleColor.DarkGray
        };

        LogFormatter.Write(Console.Out, message, includeDate: false, includeThread: false);
        Console.ForegroundColor = previous;
    }

    public void Flush() => Console.Out.Flush();

    public void Dispose() { }
}
