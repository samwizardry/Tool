namespace Tool.Collections;

public readonly record struct SlotHandle(uint Index, uint Generation)
{
    public static SlotHandle Invalid => default;
    public bool IsValid => Generation != 0;
}

/// <summary>
/// Contiguous generational resource pool with O(1) lookup.
/// </summary>
public sealed class SlotMap<T>(int initialCapacity = 64) where T : struct
{
    private struct Slot
    {
        public T Value;
        public uint Generation;
        public bool IsAlive;
    }

    private Slot[] _slots = new Slot[initialCapacity];
    private readonly Stack<uint> _freeIndices = new();
    private uint _highWaterMark;

    public int LiveCount { get; private set; }

    public SlotHandle Add(in T value)
    {
        uint index;
        if (_freeIndices.Count > 0)
        {
            index = _freeIndices.Pop();
        }
        else
        {
            index = _highWaterMark++;
            if (index >= _slots.Length)
            {
                Array.Resize(ref _slots, _slots.Length * 2);
            }
        }

        ref Slot slot = ref _slots[index];
        if (slot.Generation == 0)
        {
            slot.Generation = 1; // 0 is reserved for Invalid handle
        }

        slot.Value = value;
        slot.IsAlive = true;
        LiveCount++;

        return new SlotHandle(index, slot.Generation);
    }

    public bool IsValid(SlotHandle handle)
    {
        if (!handle.IsValid || handle.Index >= _highWaterMark)
            return false;

        ref readonly Slot slot = ref _slots[handle.Index];
        return slot.IsAlive && slot.Generation == handle.Generation;
    }

    public ref T Get(SlotHandle handle)
    {
#if TOOL_DEBUG
        if (!IsValid(handle))
            throw new InvalidOperationException($"Attempted to access invalid or destroyed handle ({handle.Index}:{handle.Generation}).");
#endif
        return ref _slots[handle.Index].Value;
    }

    public bool TryRemove(SlotHandle handle, out T value)
    {
        if (!IsValid(handle))
        {
            value = default;
            return false;
        }

        ref Slot slot = ref _slots[handle.Index];
        value = slot.Value;
        slot.Value = default; // Clear managed references if any (e.g. D3D11 COM wrappers)
        slot.IsAlive = false;
        slot.Generation++;
        if (slot.Generation == 0)
            slot.Generation = 1; // Skip 0 on wrap-around

        _freeIndices.Push(handle.Index);
        LiveCount--;
        return true;
    }

    /// <summary>
    /// Drains all remaining live entries (used during shutdown cleanup and leak reporting).
    /// </summary>
    public void DrainAll(Action<SlotHandle, T> onItem)
    {
        for (uint i = 0; i < _highWaterMark; i++)
        {
            ref Slot slot = ref _slots[i];
            if (!slot.IsAlive)
                continue;

            var handle = new SlotHandle(i, slot.Generation);
            T value = slot.Value;
            slot.Value = default;
            slot.IsAlive = false;
            slot.Generation++;
            if (slot.Generation == 0)
                slot.Generation = 1; // Skip 0 on wrap-around

            LiveCount--;
            onItem(handle, value);
        }

        _freeIndices.Clear();
        _highWaterMark = 0;
    }
}
