namespace Torture.Diagnostics;

/// <summary>Named log channel with its own runtime severity filter.</summary>
public sealed class LogCategory
{
    public LogCategory(string name, LogSeverity minimumSeverity = LogSeverity.Trace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        MinimumSeverity = minimumSeverity;
    }

    public string Name { get; }

    /// <summary>Messages below this severity are discarded for this category. Can be changed at runtime.</summary>
    public LogSeverity MinimumSeverity { get; set; }

    public override string ToString() => Name;
}

/// <summary>Engine-level categories. Backends and games declare their own.</summary>
public static class LogCategories
{
    public static readonly LogCategory Core = new("Core");
    public static readonly LogCategory Graphics = new("Graphics");
    public static readonly LogCategory Memory = new("Memory");
    public static readonly LogCategory Input = new("Input");
    public static readonly LogCategory Sdl = new("SDL");
}