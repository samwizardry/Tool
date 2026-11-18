using System.Diagnostics;

namespace Torture.Diagnostics;

public enum LogLevel
{
    Trace,
    Info,
    Warning,
    Error
}

/// <summary>
/// Framework logger. Trace and Info calls (including argument evaluation) are stripped
/// unless TORTURE_LOG is defined; warnings and errors are always emitted.
/// </summary>
public static class Log
{
    public const string Symbol = "TORTURE_LOG";

    private static readonly Lock s_lock = new();

    [Conditional(Symbol)]
    public static void Trace(string category, string message) => Write(LogLevel.Trace, category, message);

    [Conditional(Symbol)]
    public static void Info(string category, string message) => Write(LogLevel.Info, category, message);

    public static void Warning(string category, string message) => Write(LogLevel.Warning, category, message);

    public static void Error(string category, string message) => Write(LogLevel.Error, category, message);

    private static void Write(LogLevel level, string category, string message)
    {
        lock (s_lock)
        {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = level switch
            {
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Trace => ConsoleColor.DarkGray,
                _ => ConsoleColor.Gray
            };
            Console.WriteLine($"[{category} {level}] {message}");
            Console.ForegroundColor = previous;
        }
    }
}
