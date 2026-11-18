using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Torture.Diagnostics;

/// <summary>
/// Writes log lines to a UTF-8 file. The previous session's log is kept as "*.prev.log".
/// Buffered: flushes on severe messages, at most once per second otherwise, and on dispose.
/// </summary>
public sealed class FileLogSink : ILogSink
{
    private static readonly long AutoFlushIntervalTicks = Stopwatch.Frequency; // 1 second

    private readonly StreamWriter _writer;
    private long _lastFlushTimestamp;

    public FileLogSink(string path, bool keepPreviousLog = true)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            if (keepPreviousLog && File.Exists(path))
            {
                File.Move(path, Path.ChangeExtension(path, ".prev.log"), overwrite: true);
            }
            _writer = Open(path);
        }
        catch (IOException)
        {
            // The file is locked, most likely by another running instance: fall back to a per-process file.
            path = Path.ChangeExtension(path, $".{Environment.ProcessId}.log");
            _writer = Open(path);
        }

        FilePath = path;
        _writer.WriteLine(
            $"=== Log started {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {RuntimeInformation.OSDescription} | " +
            $".NET {Environment.Version} | PID {Environment.ProcessId} ===");
        _lastFlushTimestamp = Stopwatch.GetTimestamp();
    }

    public string FilePath { get; }

    public LogSeverity MinimumSeverity { get; set; } = LogSeverity.Trace;

    /// <summary>Messages at or above this severity are flushed immediately so they survive a crash.</summary>
    public LogSeverity FlushSeverity { get; set; } = LogSeverity.Error;

    public void Write(in LogMessage message)
    {
        LogFormatter.Write(_writer, message, includeDate: true, includeThread: true);

        long now = Stopwatch.GetTimestamp();
        if (message.Severity >= FlushSeverity || now - _lastFlushTimestamp >= AutoFlushIntervalTicks)
        {
            _writer.Flush();
            _lastFlushTimestamp = now;
        }
    }

    public void Flush() => _writer.Flush();

    public void Dispose()
    {
        _writer.WriteLine($"=== Log closed {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        _writer.Dispose();
    }

    private static StreamWriter Open(string path) => new(
        path,
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.Read // allows tailing the log while the game runs
        });
}
