using System.Runtime.CompilerServices;

using Brutal.Collections;

namespace Torture.Core.Collections;

/// <summary>
/// Zero-allocation unmanaged hash map for tracking subdivided edge midpoints.
/// Backed by a contiguous NativeList to guarantee deterministic cleanup.
/// </summary>
internal struct NativeEdgeMap : IDisposable
{
    private struct Entry
    {
        public uint Key;
        public ushort Value;
    }

    private NativeList<Entry> _table;
    private readonly int _mask;

    public NativeEdgeMap(int estimatedEdgeCount)
    {
        // Capacity must be power of two for fast bitwise masking
        int capacity = 16;
        while (capacity < estimatedEdgeCount * 2)
        {
            capacity <<= 1;
        }

        _mask = capacity - 1;
        _table = [];
        _table.EnsureCapacity(capacity);
        for (int i = 0; i < capacity; ++i)
        {
            _table.Add(default);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetValue(uint key, out ushort value)
    {
        // Knuth multiplicative hash
        int slot = (int)((key * 2654435761u) & (uint)_mask);

        while (true)
        {
            ref readonly Entry entry = ref _table[slot];
            if (entry.Key == key)
            {
                value = entry.Value;
                return true;
            }

            if (entry.Key == 0)
            {
                value = 0;
                return false;
            }

            slot = (slot + 1) & _mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Add(uint key, ushort value)
    {
        int slot = (int)((key * 2654435761u) & (uint)_mask);

        while (true)
        {
            ref Entry entry = ref _table[slot];
            if (entry.Key == 0)
            {
                entry.Key = key;
                entry.Value = value;
                return;
            }

            if (entry.Key == key)
            {
                entry.Value = value;
                return;
            }

            slot = (slot + 1) & _mask;
        }
    }

    public void Dispose()
    {
        _table.Dispose();
    }
}