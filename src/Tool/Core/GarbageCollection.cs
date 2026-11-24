using System.Diagnostics;
using System.Runtime;

using Tool.Logging;

namespace Tool.Core;

public static class GarbageCollection
{
    /// <summary>
    /// Full blocking compacting collection. Use on loading screens only.
    /// </summary>
    public static void FullBlockingCollect()
    {
#if TRACK_ALLOCATIONS
        long before = GC.GetTotalMemory(false);
        long start = Stopwatch.GetTimestamp();
#endif

        // LOH is not compacted by default; CompactOnce resets itself after the next full blocking GC.
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

        // Pass 1: unreachable finalizable objects are moved to the finalization queue.
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        // Wait for the finalizer thread (NativeList, COM wrappers, etc.).
        GC.WaitForPendingFinalizers();
        // Pass 2: collect objects released by finalizers.
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

#if TRACK_ALLOCATIONS
        LogStats(before, Stopwatch.GetElapsedTime(start));
#endif
    }

    /// <summary>
    /// Gameplay mode: avoids blocking gen2 collections when possible.
    /// </summary>
    public static void EnterGameplayMode()
        => GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

    /// <summary>
    /// Loading mode: lets the GC work aggressively.
    /// </summary>
    public static void EnterLoadingMode()
        => GCSettings.LatencyMode = GCLatencyMode.Interactive;

    private static void LogStats(long before, TimeSpan elapsed)
    {
        long after = GC.GetTotalMemory(false);

        Log.Info(LogCategories.Memory,
            $"GC: full collect {before / 1024} KB -> {after / 1024} KB in {elapsed.TotalMilliseconds:F2} ms; " +
            $"gen0={GC.CollectionCount(0)} gen1={GC.CollectionCount(1)} gen2={GC.CollectionCount(2)}");
    }
}
