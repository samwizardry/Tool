using System.Globalization;
using System.Runtime.CompilerServices;

namespace Torture.Diagnostics;

/// <summary>Compile-time severity marker used by <see cref="LogInterpolatedStringHandler{TLevel}"/>.</summary>
public interface ILogLevel
{
    static abstract LogSeverity Severity { get; }
}

/// <summary>Severity markers for interpolated log calls.</summary>
public static class LogLevels
{
    public readonly struct Trace : ILogLevel { public static LogSeverity Severity => LogSeverity.Trace; }
    public readonly struct Debug : ILogLevel { public static LogSeverity Severity => LogSeverity.Debug; }
    public readonly struct Info : ILogLevel { public static LogSeverity Severity => LogSeverity.Info; }
    public readonly struct Warning : ILogLevel { public static LogSeverity Severity => LogSeverity.Warning; }
    public readonly struct Error : ILogLevel { public static LogSeverity Severity => LogSeverity.Error; }
    public readonly struct Fatal : ILogLevel { public static LogSeverity Severity => LogSeverity.Fatal; }
}

/// <summary>
/// Interpolated string handler that skips formatting (and evaluation of interpolation holes)
/// when the message would be filtered out.
/// </summary>
[InterpolatedStringHandler]
public ref struct LogInterpolatedStringHandler<TLevel>
    where TLevel : struct, ILogLevel
{
    private DefaultInterpolatedStringHandler _builder;

    public LogInterpolatedStringHandler(int literalLength, int formattedCount, LogCategory category, out bool isEnabled)
    {
        IsEnabled = isEnabled = Log.IsEnabled(TLevel.Severity, category);
        _builder = isEnabled
            ? new DefaultInterpolatedStringHandler(literalLength, formattedCount, CultureInfo.InvariantCulture)
            : default;
    }

    public bool IsEnabled { get; }

    public void AppendLiteral(string value) => _builder.AppendLiteral(value);

    public void AppendFormatted<T>(T value) => _builder.AppendFormatted(value);

    public void AppendFormatted<T>(T value, string? format) => _builder.AppendFormatted(value, format);

    public void AppendFormatted<T>(T value, int alignment) => _builder.AppendFormatted(value, alignment);

    public void AppendFormatted<T>(T value, int alignment, string? format) => _builder.AppendFormatted(value, alignment, format);

    public void AppendFormatted(scoped ReadOnlySpan<char> value) => _builder.AppendFormatted(value);

    public void AppendFormatted(string? value) => _builder.AppendFormatted(value);

    internal string ToStringAndClear() => _builder.ToStringAndClear();
}