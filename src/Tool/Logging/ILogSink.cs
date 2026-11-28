namespace Tool.Logging;

public interface ILogSink : IDisposable
{
    LogSeverity MinimumSeverity { get; }

    void Write(in LogMessage message);

    void Flush();
}
