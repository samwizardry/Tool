#if PLATFORM_WINDOWS
using System.Runtime.InteropServices;
#endif

namespace Torture.Logging.Sinks;

public sealed partial class ConsoleLogSink : ILogSink
{
    private readonly bool _useColors;

    public ConsoleLogSink(LogSeverity minimumSeverity = LogSeverity.Message)
    {
        MinimumSeverity = minimumSeverity;
        _useColors = !Console.IsOutputRedirected && TryEnableAnsiColors();
    }

    public LogSeverity MinimumSeverity { get; }

    public void Write(in LogMessage message)
        => LogFormatter.Write(Console.Out, message, includeDate: false, includeThread: false, useColors: _useColors);

    public void Flush() => Console.Out.Flush();

    public void Dispose() { }

    private static bool TryEnableAnsiColors()
    {
#if PLATFORM_WINDOWS
        const int StdOutputHandle = -11;
        const uint EnableVirtualTerminalProcessing = 0x0004;

        nint handle = GetStdHandle(StdOutputHandle);
        if (handle == 0 || handle == -1 || !GetConsoleMode(handle, out uint mode))
        {
            return false;
        }

        return (mode & EnableVirtualTerminalProcessing) != 0
            || SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
#else
        return true;
#endif
    }

#if PLATFORM_WINDOWS
    [LibraryImport("kernel32.dll")]
    private static partial nint GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(nint hConsoleHandle, uint dwMode);
#endif
}