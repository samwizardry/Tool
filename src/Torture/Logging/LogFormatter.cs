using System.Globalization;

namespace Torture.Logging;

public static class LogFormatter
{
    public static string SeverityTag(LogSeverity severity) => severity switch
    {
        LogSeverity.Message => "MSG",
        LogSeverity.Info => "INF",
        LogSeverity.Warning => "WRN",
        LogSeverity.Error => "ERR",
        LogSeverity.Fatal => "FTL",
        _ => "???"
    };

    public static void Write(TextWriter writer, in LogMessage message, bool includeDate, bool includeThread)
    {
        Span<char> buffer = stackalloc char[512];
        int pos = 0;

        DateTime local = message.Timestamp.ToLocalTime();
        string dateFormat = includeDate ? "yyyy-MM-dd HH:mm:ss.fff" : "HH:mm:ss.fff";
        if (local.TryFormat(buffer[pos..], out int written, dateFormat, CultureInfo.InvariantCulture))
        {
            pos += written;
        }

        TryAppend(buffer, ref pos, " ");
        TryAppend(buffer, ref pos, SeverityTag(message.Severity));
        TryAppend(buffer, ref pos, " [");
        TryAppend(buffer, ref pos, message.Category.Name);

        if (message.Id != 0)
        {
            TryAppend(buffer, ref pos, " #");
            if (message.Id.TryFormat(buffer[pos..], out written, provider: CultureInfo.InvariantCulture))
            {
                pos += written;
            }
        }

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

        if (TryAppend(buffer, ref pos, message.Text))
        {
            writer.WriteLine(buffer[..pos]);
        }
        else
        {
            writer.Write(buffer[..pos]);
            writer.WriteLine(message.Text);
        }

        if (message.Exception != null)
        {
            writer.WriteLine(message.Exception.ToString());
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