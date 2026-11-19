using SDL3;

namespace Torture.Diagnostics;

/// <summary>Routes SDL's internal log output into <see cref="Log"/> under <see cref="LogCategories.Sdl"/>.</summary>
internal static class SdlLogBridge
{
    // SDL keeps a native pointer to this delegate: it must stay referenced for the whole process lifetime.
    private static readonly SDL.LogOutputFunction s_callback = OnSdlLog;

    public static void Install()
    {
        SDL.SetLogOutputFunction(s_callback, IntPtr.Zero);
        // Let SDL pre-filter at the source; by default most SDL categories only report errors.
#if TORTURE_LOG
        SDL.SetLogPriorities(SDL.LogPriority.Info);
#else
        SDL.SetLogPriorities(SDL.LogPriority.Warn);
#endif
    }

    /// <summary>Restores SDL's default output so late SDL messages after Log.Shutdown still go somewhere.</summary>
    public static void Uninstall() => SDL.SetLogOutputFunction(SDL.GetDefaultLogOutputFunction(), IntPtr.Zero);

    private static void OnSdlLog(IntPtr userdata, SDL.LogCategory category, SDL.LogPriority priority, string message)
    {
        try
        {
            LogSeverity severity = ToSeverity(priority);
            if (Log.IsEnabled(severity, LogCategories.Sdl))
            {
                Log.Write(severity, LogCategories.Sdl, $"{CategoryName(category)}: {message}");
            }
        }
        catch
        {
            // Exceptions must never cross the native boundary.
        }
    }

    private static LogSeverity ToSeverity(SDL.LogPriority priority) => priority switch
    {
        SDL.LogPriority.Critical => LogSeverity.Fatal,
        SDL.LogPriority.Error => LogSeverity.Error,
        SDL.LogPriority.Warn => LogSeverity.Warning,
        SDL.LogPriority.Info => LogSeverity.Info,
        SDL.LogPriority.Debug => LogSeverity.Debug,
        _ => LogSeverity.Trace // SDL.LogPriority.Verbose, SDL.LogPriority.Trace, SDL.LogPriority.Invalid
    };

    private static string CategoryName(SDL.LogCategory category) => category switch
    {
        SDL.LogCategory.Application => "App",
        SDL.LogCategory.Error => "Error",
        SDL.LogCategory.Assert => "Assert",
        SDL.LogCategory.System => "System",
        SDL.LogCategory.Audio => "Audio",
        SDL.LogCategory.Video => "Video",
        SDL.LogCategory.Render => "Render",
        SDL.LogCategory.Input => "Input",
        SDL.LogCategory.Test => "Test",
        SDL.LogCategory.GPU => "GPU",
        _ => "Custom"
    };
}