using System.Globalization;

namespace Tool.Logging;

/// <summary>Serilog-style single-line log formatter with optional zero-allocation ANSI colors.</summary>
public static class LogFormatter
{
    private const string AnsiReset = "\e[0m";
    private const string AnsiDarkGray = "\e[90m";
    private const string AnsiGray = "\e[37m";
    private const string AnsiWhite = "\e[97m";
    private const string AnsiCyan = "\e[36m";
    private const string AnsiBrightMagenta = "\e[95m";
    private const string AnsiBrightYellow = "\e[93m";
    private const string AnsiBrightRed = "\e[91m";
    private const string AnsiWhiteOnRed = "\e[97;41m";
    private const string AnsiWhiteOnMagenta = "\e[97;45m";

    public static string SeverityTag(LogSeverity severity) => severity switch
    {
        LogSeverity.Message => "MSG",
        LogSeverity.Info => "INF",
        LogSeverity.Warning => "WRN",
        LogSeverity.Error => "ERR",
        LogSeverity.Fatal => "FTL",
        _ => "???"
    };

    private static string SeverityColor(LogSeverity severity) => severity switch
    {
        LogSeverity.Fatal => AnsiWhiteOnMagenta,
        LogSeverity.Error => AnsiWhiteOnRed,
        LogSeverity.Warning => AnsiBrightYellow,
        LogSeverity.Info => AnsiWhite,
        LogSeverity.Message => AnsiDarkGray,
        _ => AnsiGray
    };

    public static void Write(
        TextWriter writer,
        in LogMessage message,
        bool includeDate = false,
        bool includeThread = false,
        bool useColors = false)
    {
        Span<char> buffer = stackalloc char[512];
        int pos = 0;

        // [HH:mm:ss.fff INF]
        AppendColor(buffer, ref pos, useColors, AnsiDarkGray);
        TryAppend(buffer, ref pos, "[");

        AppendColor(buffer, ref pos, useColors, AnsiGray);
        DateTime local = message.Timestamp.ToLocalTime();
        string dateFormat = includeDate ? "yyyy-MM-dd HH:mm:ss.fff" : "HH:mm:ss.fff";
        if (local.TryFormat(buffer[pos..], out int written, dateFormat, CultureInfo.InvariantCulture))
        {
            pos += written;
        }

        TryAppend(buffer, ref pos, " ");
        AppendColor(buffer, ref pos, useColors, SeverityColor(message.Severity));
        TryAppend(buffer, ref pos, SeverityTag(message.Severity));

        // Reset first to clear background colors on Error/Fatal before closing bracket.
        AppendColor(buffer, ref pos, useColors, AnsiReset);
        AppendColor(buffer, ref pos, useColors, AnsiDarkGray);
        TryAppend(buffer, ref pos, "] [");

        // [Category #Id]
        AppendColor(buffer, ref pos, useColors, AnsiCyan);
        TryAppend(buffer, ref pos, message.Category.Name);

        if (message.Id != 0)
        {
            AppendColor(buffer, ref pos, useColors, AnsiBrightMagenta);
            TryAppend(buffer, ref pos, " #");
            if (message.Id.TryFormat(buffer[pos..], out written, provider: CultureInfo.InvariantCulture))
            {
                pos += written;
            }
        }

        AppendColor(buffer, ref pos, useColors, AnsiDarkGray);
        TryAppend(buffer, ref pos, "] ");

        if (includeThread)
        {
            TryAppend(buffer, ref pos, "(T");
            if (message.ThreadId.TryFormat(buffer[pos..], out written, provider: CultureInfo.InvariantCulture))
            {
                pos += written;
            }
            TryAppend(buffer, ref pos, ") ");
        }

        // Message body: dim gray for Message, default terminal color for Info..Fatal.
        AppendColor(buffer, ref pos, useColors, message.Severity == LogSeverity.Message ? AnsiGray : AnsiReset);

        int reserved = useColors ? AnsiReset.Length : 0;
        if (message.Text.Length <= buffer.Length - pos - reserved)
        {
            TryAppend(buffer, ref pos, message.Text);
            AppendColor(buffer, ref pos, useColors, AnsiReset);
            writer.WriteLine(buffer[..pos]);
        }
        else
        {
            // Long message fallback: still zero heap allocations.
            writer.Write(buffer[..pos]);
            writer.Write(message.Text);
            if (useColors)
            {
                writer.WriteLine(AnsiReset);
            }
            else
            {
                writer.WriteLine();
            }
        }

        if (message.Exception != null)
        {
            if (useColors)
            {
                writer.Write(AnsiBrightRed);
                writer.Write(message.Exception.ToString());
                writer.WriteLine(AnsiReset);
            }
            else
            {
                writer.WriteLine(message.Exception.ToString());
            }
        }
    }

    private static void AppendColor(Span<char> destination, ref int pos, bool useColors, string ansiCode)
    {
        if (useColors)
        {
            TryAppend(destination, ref pos, ansiCode);
        }
    }

    private static bool TryAppend(Span<char> destination, ref int pos, ReadOnlySpan<char> value)
    {
        if (!value.TryCopyTo(destination[pos..]))
        {
            return false;
        }

        pos += value.Length;
        return true;
    }
}
