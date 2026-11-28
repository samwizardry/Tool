using System.Diagnostics;

namespace Tool.Logging;

public static class Log
{
    public const string Symbol = "TOOL_LOG";
    public const string SymbolMessage = "TOOL_LOG_MESSAGE";
    public const string SymbolInfo = "TOOL_LOG_INFO";
    public const string SymbolWarning = "TOOL_LOG_WARNING";
    public const string SymbolError = "TOOL_LOG_ERROR";
    public const string SymbolFatal = "TOOL_LOG_FATAL";

    public const LogSeverity MinimumSeverity =
#if TOOL_LOG_MESSAGE
        LogSeverity.Message;
#elif TOOL_LOG_INFO
        LogSeverity.Info;
#elif TOOL_LOG_WARNING
        LogSeverity.Warning;
#elif TOOL_LOG_ERROR
        LogSeverity.Error;
#elif TOOL_LOG_FATAL
        LogSeverity.Fatal;
#else
        LogSeverity.None;
#endif

    private static readonly Lock Lock = new();
    private static volatile ILogSink[] s_sinks = [];

    [ThreadStatic]
    private static bool s_isDispatching;

    public static void AddSink(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        lock (Lock)
        {
            s_sinks = [.. s_sinks, sink];
        }
    }

    public static bool RemoveSink(ILogSink sink)
    {
        lock (Lock)
        {
            int index = Array.IndexOf(s_sinks, sink);
            if (index < 0)
            {
                return false;
            }

            s_sinks = [.. s_sinks[..index], .. s_sinks[(index + 1)..]];
            return true;
        }
    }

    public static bool IsEnabled(LogSeverity severity, LogCategory category)
        => severity < LogSeverity.None
        && severity >= MinimumSeverity
        && severity >= category.MinimumSeverity
        && s_sinks.Length != 0;

    [Conditional(Symbol)]
    public static void Write(LogSeverity severity, LogCategory category, string text, int id = 0, Exception? exception = null)
        => WriteCore(severity, category, text, id, exception);

    [Conditional(SymbolMessage)]
    public static void Message(LogCategory category, string text)
        => WriteCore(LogSeverity.Message, category, text);

    [Conditional(SymbolInfo)]
    public static void Info(LogCategory category, string text)
        => WriteCore(LogSeverity.Info, category, text);

    [Conditional(SymbolWarning)]
    public static void Warning(LogCategory category, string text)
        => WriteCore(LogSeverity.Warning, category, text);

    [Conditional(SymbolError)]
    public static void Error(LogCategory category, string text, Exception? exception = null)
        => WriteCore(LogSeverity.Error, category, text, exception: exception);

    [Conditional(SymbolFatal)]
    public static void Fatal(LogCategory category, string text, Exception? exception = null)
        => WriteCore(LogSeverity.Fatal, category, text, exception: exception);

    private static void WriteCore(LogSeverity severity, LogCategory category, string text, int id = 0, Exception? exception = null)
    {
        // A sink that logs would recurse into itself; such messages are dropped before acquiring the lock.
        if (s_isDispatching || !IsEnabled(severity, category))
        {
            return;
        }

        var message = new LogMessage(DateTime.UtcNow, severity, category, text, id, Environment.CurrentManagedThreadId, exception);

        lock (Lock)
        {
            s_isDispatching = true;
            try
            {
                foreach (ILogSink sink in s_sinks)
                {
                    if (severity < sink.MinimumSeverity)
                    {
                        continue;
                    }

                    try
                    {
                        sink.Write(in message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Log] {sink.GetType().Name} failed: {ex}");
                    }
                }
            }
            finally
            {
                s_isDispatching = false;
            }
        }
    }

    public static void Flush()
    {
        lock (Lock)
        {
            foreach (ILogSink sink in s_sinks)
            {
                try
                {
                    sink.Flush();
                }
                catch { }
            }
        }
    }

    public static void Shutdown()
    {
        lock (Lock)
        {
            foreach (ILogSink sink in s_sinks)
            {
                try
                {
                    sink.Flush();
                    sink.Dispose();
                }
                catch { }
            }

            s_sinks = [];
        }
    }
}