using System.Threading.Channels;

namespace Torture.Diagnostics;

/// <summary>
/// Decorator that moves the inner sink's I/O to a background worker.
/// Callers only enqueue; the inner sink is touched by the worker and by Flush/Dispose under one lock,
/// so it does not need to be thread-safe.
/// </summary>
public sealed class BackgroundLogSink : ILogSink
{
    private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(2);

    private readonly ILogSink _inner;
    private readonly Channel<LogMessage> _queue;
    private readonly Lock _innerLock = new();
    private readonly Task _worker;
    private int _pending;
    private int _dropped;
    private bool _disposed;

    public BackgroundLogSink(ILogSink inner, int capacity = 8192)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _queue = Channel.CreateBounded<LogMessage>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            // With Wait mode TryWrite returns false when the queue is full, so drops can be counted.
            FullMode = BoundedChannelFullMode.Wait
        });
        _worker = Task.Run(ProcessQueueAsync);
    }

    /// <summary>Delegates to the inner sink so filtering happens before enqueueing.</summary>
    public LogSeverity MinimumSeverity => _inner.MinimumSeverity;

    /// <summary>Messages at or above this severity block the caller until the queue is drained (crash safety).</summary>
    public LogSeverity DrainSeverity { get; set; } = LogSeverity.Fatal;

    public void Write(in LogMessage message)
    {
        Interlocked.Increment(ref _pending);
        if (!_queue.Writer.TryWrite(message))
        {
            Interlocked.Decrement(ref _pending);
            Interlocked.Increment(ref _dropped);
            return;
        }

        if (message.Severity >= DrainSeverity)
        {
            Flush();
        }
    }

    public void Flush()
    {
        // Wait until the worker has written everything enqueued so far, then flush the inner sink.
        SpinWait.SpinUntil(() => Volatile.Read(ref _pending) == 0, DrainTimeout);
        lock (_innerLock)
        {
            _inner.Flush();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        _queue.Writer.TryComplete();
        _worker.Wait(DrainTimeout);

        lock (_innerLock)
        {
            ReportDropped();
            _inner.Flush();
            _inner.Dispose();
        }
    }

    private async Task ProcessQueueAsync()
    {
        ChannelReader<LogMessage> reader = _queue.Reader;
        while (await reader.WaitToReadAsync().ConfigureAwait(false))
        {
            DrainAvailable(reader);
        }
    }

    private void DrainAvailable(ChannelReader<LogMessage> reader)
    {
        lock (_innerLock)
        {
            while (reader.TryRead(out LogMessage message))
            {
                try
                {
                    _inner.Write(in message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Log] {_inner.GetType().Name} failed: {ex}");
                }
                finally
                {
                    Interlocked.Decrement(ref _pending);
                }
            }

            ReportDropped();
        }
    }

    // Must be called under _innerLock.
    private void ReportDropped()
    {
        int dropped = Interlocked.Exchange(ref _dropped, 0);
        if (dropped > 0)
        {
            var warning = new LogMessage(DateTime.UtcNow, LogSeverity.Warning, LogCategories.Core,
                $"Log: {dropped} message(s) dropped, background queue is full.", 0, Environment.CurrentManagedThreadId, null);
            _inner.Write(in warning);
        }
    }
}