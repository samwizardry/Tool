using System.Diagnostics;
using System.Text;

namespace Torture.Core.Diagnostics;

/// <summary>
/// Per-frame managed allocation tracker for the main thread.
/// Calls are stripped unless TRACK_ALLOCATIONS is defined.
/// </summary>
public static class AllocationTracker
{
    private const int MaxMarks = 32;
    private const int MaxDetailedLinesPerSecond = 5;
    private static readonly long WarmupTicks = Stopwatch.Frequency * 2; // 2 seconds of JIT warm-up

    private static readonly string[] s_labels = new string[MaxMarks];
    private static readonly long[] s_bytes = new long[MaxMarks];
    private static readonly StringBuilder s_sb = new(256);
    private static int s_count;

    private static long s_last;
    private static long s_frameEnd = -1;
    private static ulong s_frame;

    private static long s_startTimestamp;
    private static long s_secondStart;
    private static int s_linesThisSecond;
    private static int s_framesThisSecond;
    private static int s_allocFramesThisSecond;
    private static long s_bytesThisSecond;

    private static int s_gen0, s_gen1, s_gen2;

    public static long LastFrameBytes { get; private set; }
    public static long BytesLastSecond { get; private set; }

    /// <summary>Call once before the main loop starts (after loading and the full GC).</summary>
    [Conditional("TRACK_ALLOCATIONS")]
    public static void Reset()
    {
        s_startTimestamp = s_secondStart = Stopwatch.GetTimestamp();
        s_frameEnd = -1;
        s_frame = 0;
        s_linesThisSecond = s_framesThisSecond = s_allocFramesThisSecond = 0;
        s_bytesThisSecond = 0;
        s_gen0 = GC.CollectionCount(0);
        s_gen1 = GC.CollectionCount(1);
        s_gen2 = GC.CollectionCount(2);
    }

    [Conditional("TRACK_ALLOCATIONS")]
    public static void BeginFrame()
    {
        s_count = 0;
        s_last = GC.GetAllocatedBytesForCurrentThread();

        // Everything allocated between frames (AppEvent, etc.).
        if (s_frameEnd >= 0)
        {
            Record("Events", s_last - s_frameEnd);
        }
    }

    /// <summary>Attributes bytes allocated since the previous mark to this label. Use string literals only.</summary>
    [Conditional("TRACK_ALLOCATIONS")]
    public static void Mark(string label)
    {
        long now = GC.GetAllocatedBytesForCurrentThread();
        Record(label, now - s_last);
        s_last = now;
    }

    [Conditional("TRACK_ALLOCATIONS")]
    public static void EndFrame()
    {
        long total = 0;
        for (int i = 0; i < s_count; i++)
        {
            total += s_bytes[i];
        }

        LastFrameBytes = total;
        s_frame++;

        long now = Stopwatch.GetTimestamp();
        bool warmedUp = now - s_startTimestamp >= WarmupTicks;

        if (warmedUp)
        {
            s_framesThisSecond++;
            if (total > 0)
            {
                s_allocFramesThisSecond++;
                s_bytesThisSecond += total;
                if (s_linesThisSecond++ < MaxDetailedLinesPerSecond)
                {
                    ReportFrame(total);
                }
            }
        }

        ReportCollections(print: warmedUp);

        if (now - s_secondStart >= Stopwatch.Frequency)
        {
            if (warmedUp && s_allocFramesThisSecond > 0)
            {
                Console.WriteLine(
                    $"[Alloc] last second: {s_allocFramesThisSecond}/{s_framesThisSecond} frames allocated, {s_bytesThisSecond} B total");
            }

            BytesLastSecond = s_bytesThisSecond;
            s_secondStart = now;
            s_linesThisSecond = s_framesThisSecond = s_allocFramesThisSecond = 0;
            s_bytesThisSecond = 0;
        }

        // Must be the last statement: logger allocations must not leak into the next frame.
        s_frameEnd = GC.GetAllocatedBytesForCurrentThread();
    }

    private static void Record(string label, long bytes)
    {
        if (s_count < MaxMarks)
        {
            s_labels[s_count] = label;
            s_bytes[s_count] = bytes;
            s_count++;
        }
    }

    private static void ReportFrame(long total)
    {
        s_sb.Clear().Append("[Alloc] frame ").Append(s_frame).Append(": ").Append(total).Append(" B |");
        for (int i = 0; i < s_count; i++)
        {
            if (s_bytes[i] != 0)
            {
                s_sb.Append(' ').Append(s_labels[i]).Append('=').Append(s_bytes[i]).Append(" B");
            }
        }
        Console.WriteLine(s_sb.ToString());
    }

    private static void ReportCollections(bool print)
    {
        int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
        if (print && g0 != s_gen0)
        {
            Console.WriteLine(
                $"[GC] frame {s_frame}: gen0 +{g0 - s_gen0}, gen1 +{g1 - s_gen1}, gen2 +{g2 - s_gen2}, " +
                $"total pause {GC.GetTotalPauseDuration().TotalMilliseconds:F2} ms");
        }
        s_gen0 = g0; s_gen1 = g1; s_gen2 = g2;
    }
}