using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tool.Collections;

public static class Extensions
{
    /// <summary>
    /// Gets a Span<T> view over the data in a list. Items should not be added or removed from the List<T> while the Span<T> is in use.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpan<T>(this List<T> list)
    {
        return CollectionsMarshal.AsSpan(list);
    }
}
