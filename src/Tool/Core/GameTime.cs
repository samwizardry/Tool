namespace Tool.Core;

/// <summary>
/// Holds the time state of the application loop.
/// </summary>
public readonly record struct GameTime
{
    public readonly TimeSpan TotalGameTime;
    public readonly TimeSpan ElapsedGameTime;

    public GameTime(TimeSpan totalGameTime, TimeSpan elapsedGameTime)
    {
        TotalGameTime = totalGameTime;
        ElapsedGameTime = elapsedGameTime;
    }

    /// <summary>
    /// Elapsed time since the previous frame in seconds.
    /// </summary>
    public float DeltaSeconds => (float)ElapsedGameTime.TotalSeconds;
    /// <summary>
    /// Total time elapsed since the application started in seconds.
    /// </summary>
    public float TotalSeconds => (float)TotalGameTime.TotalSeconds;
}
