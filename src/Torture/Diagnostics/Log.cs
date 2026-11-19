using System.Runtime.CompilerServices;

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
    public static void Trace(LogCategory category, [InterpolatedStringHandlerArgument(nameof(category))] ref LogInterpolatedStringHandler<LogLevels.Trace> text)
    {
        if (text.IsEnabled)
        {
            Write(LogSeverity.Trace, category, text.ToStringAndClear());
        }
    }

    [System.Diagnostics.Conditional(Symbol)]
    public static void Debug(LogCategory category, [InterpolatedStringHandlerArgument(nameof(category))] ref LogInterpolatedStringHandler<LogLevels.Debug> text)
    {
        if (text.IsEnabled)
        {
            Write(LogSeverity.Debug, category, text.ToStringAndClear());
        }
    }

    public static void Info(LogCategory category, [InterpolatedStringHandlerArgument(nameof(category))] ref LogInterpolatedStringHandler<LogLevels.Info> text)
    {
        if (text.IsEnabled)
        {
            Write(LogSeverity.Info, category, text.ToStringAndClear());
        }
    }

    public static void Warning(LogCategory category, [InterpolatedStringHandlerArgument(nameof(category))] ref LogInterpolatedStringHandler<LogLevels.Warning> text)
    {
        if (text.IsEnabled)
        {
            Write(LogSeverity.Warning, category, text.ToStringAndClear());
        }
    }

    public static void Error(LogCategory category, [InterpolatedStringHandlerArgument(nameof(category))] ref LogInterpolatedStringHandler<LogLevels.Error> text, Exception? exception = null)
    {
        if (text.IsEnabled)
        {
            Write(LogSeverity.Error, category, text.ToStringAndClear(), exception: exception);
        }
    }

    public static void Fatal(LogCategory category, [InterpolatedStringHandlerArgument(nameof(category))] ref LogInterpolatedStringHandler<LogLevels.Fatal> text, Exception? exception = null)
    {
        if (text.IsEnabled)
        {
            Write(LogSeverity.Fatal, category, text.ToStringAndClear(), exception: exception);
        }
    }

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
