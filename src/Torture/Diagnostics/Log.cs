namespace Torture.Diagnostics;

/// <summary>
/// Static logging hub. It does not write anywhere by itself: messages are dispatched to registered sinks.
/// Thread-safe. Trace and Debug calls are stripped at compile time unless TORTURE_LOG is defined.
/// </summary>
public static class Log
{
    public const string Symbol = "TORTURE_LOG";

    private static readonly Lock s_lock = new();
    private static ILogSink[] s_sinks = [];

    [ThreadStatic]
    private static bool s_isDispatching;

    /// <summary>Global runtime filter, checked before category and sink filters.</summary>
    public static LogSeverity MinimumSeverity { get; set; } =
#if TORTURE_LOG
        LogSeverity.Trace;
#else
        LogSeverity.Info;
#endif

    public static void AddSink(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        lock (s_lock)
        {
            s_sinks = [.. s_sinks, sink];
        }
    }

    public static bool RemoveSink(ILogSink sink)
    {
        lock (s_lock)
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

    /// <summary>Cheap pre-check to skip expensive message formatting.</summary>
    public static bool IsEnabled(LogSeverity severity, LogCategory category)
        => severity >= MinimumSeverity
        && severity >= category.MinimumSeverity
        && s_sinks.Length != 0;

    public static void Write(LogSeverity severity, LogCategory category, string text, int id = 0, Exception? exception = null)
    {
        if (!IsEnabled(severity, category))
        {
            return;
        }

        var message = new LogMessage(DateTime.UtcNow, severity, category, text, id, Environment.CurrentManagedThreadId, exception);

        lock (s_lock)
        {
            // A sink that logs would recurse into itself; such messages are dropped.
            if (s_isDispatching)
            {
                return;
            }

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
                        // A broken sink must never crash the game.
                        System.Diagnostics.Debug.WriteLine($"[Log] {sink.GetType().Name} failed: {ex}");
                    }
                }
            }
            finally
            {
                s_isDispatching = false;
            }
        }
    }

    [System.Diagnostics.Conditional(Symbol)]
    public static void Trace(LogCategory category, string text) => Write(LogSeverity.Trace, category, text);

    [System.Diagnostics.Conditional(Symbol)]
    public static void Debug(LogCategory category, string text) => Write(LogSeverity.Debug, category, text);

    public static void Info(LogCategory category, string text) => Write(LogSeverity.Info, category, text);

    public static void Warning(LogCategory category, string text) => Write(LogSeverity.Warning, category, text);

    public static void Error(LogCategory category, string text, Exception? exception = null)
        => Write(LogSeverity.Error, category, text, exception: exception);

    public static void Fatal(LogCategory category, string text, Exception? exception = null)
        => Write(LogSeverity.Fatal, category, text, exception: exception);

    public static void Flush()
    {
        lock (s_lock)
        {
            foreach (ILogSink sink in s_sinks)
            {
                try { sink.Flush(); } catch { /* ignored on purpose */ }
            }
        }
    }

    /// <summary>Flushes and disposes all sinks. Call once at application exit.</summary>
    public static void Shutdown()
    {
        lock (s_lock)
        {
            foreach (ILogSink sink in s_sinks)
            {
                try { sink?.Flush(); sink?.Dispose(); } catch { /* ignored on purpose */ }
            }
            s_sinks = [];
        }
    }
}
