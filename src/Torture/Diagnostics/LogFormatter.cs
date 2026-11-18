using System.Globalization;

namespace Torture.Diagnostics;

/// <summary>Shared single-line text format for text-based sinks.</summary>
public static class LogFormatter
{
    public static string SeverityTag(LogSeverity severity) => severity switch
    {
        LogSeverity.Trace => "TRACE",
        LogSeverity.Debug => "DEBUG",
        LogSeverity.Info => "INFO ",
        LogSeverity.Warning => "WARN ",
        LogSeverity.Error => "ERROR",
        LogSeverity.Fatal => "FATAL",
        _ => "?????"
    };

    // Output: 2026-11-18 16:22:19.123 WARN  [D3D11 #342] (T1) STATE_CREATION: ...
    public static void Write(TextWriter writer, in LogMessage message, bool includeDate, bool includeThread)
    {
        DateTime local = message.Timestamp.ToLocalTime();
        writer.Write(local.ToString(includeDate ? "yyyy-MM-dd HH:mm:ss.fff" : "HH:mm:ss.fff", CultureInfo.InvariantCulture));
        writer.Write(' ');
        writer.Write(SeverityTag(message.Severity));
        writer.Write(" [");
        writer.Write(message.Category.Name);
        if (message.Id != 0)
        {
            writer.Write(" #");
            writer.Write(message.Id.ToString(CultureInfo.InvariantCulture));
        }
        writer.Write("] ");
        if (includeThread)
        {
            writer.Write("(T");
            writer.Write(message.ThreadId.ToString(CultureInfo.InvariantCulture));
            writer.Write(") ");
        }
        writer.WriteLine(message.Text);

        if (message.Exception is not null)
        {
            writer.WriteLine(message.Exception.ToString());
        }
    }
}
