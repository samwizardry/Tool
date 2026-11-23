namespace Torture.Logging;

public sealed class LogCategory
{
    public LogCategory(string name, LogSeverity minimumSeverity = LogSeverity.Message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        MinimumSeverity = minimumSeverity;
    }

    public string Name { get; }

    public LogSeverity MinimumSeverity { get; set; }

    public override string ToString() => Name;
}

public static class LogCategories
{
    public static readonly LogCategory Core = new("Core");
    public static readonly LogCategory Memory = new("Memory");
    public static readonly LogCategory Graphics = new("Graphics");
    public static readonly LogCategory Assets = new("Assets");
}