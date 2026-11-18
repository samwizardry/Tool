namespace Torture.Diagnostics;

/// <summary>Log output target. Calls are serialized by <see cref="Log"/>, so implementations need not be thread-safe.</summary>
public interface ILogSink : IDisposable
{
    /// <summary>Messages below this severity are not delivered to the sink.</summary>
    LogSeverity MinimumSeverity { get; }

    /// <summary>Writes a message. Must not call <see cref="Log"/> (such messages are dropped).</summary>
    void Write(in LogMessage message);

    void Flush();
}
