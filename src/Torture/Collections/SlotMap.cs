using Torture.Graphics;

namespace Torture.Collections;

/// <summary>
/// Contiguous generational resource pool with O(1) lookup, allocation, and free.
/// </summary>
public sealed class SlotMap<T> where T : struct
{
    private struct Slot
    {
        public T Value;
        public uint Generation;
        public bool IsAlive;
    }

    private Slot[] _slots;
    private readonly Stack<uint> _freeIndices = new();
    private uint _highWaterMark;

    public int LiveCount { get; private set; }

    public SlotMap(int initialCapacity = 64)
    {
        _slots = new Slot[initialCapacity];
    }

    public ResourceHandle Add(in T value)
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

        return new ResourceHandle(index, slot.Generation);
    }

    public bool IsValid(ResourceHandle handle)
    {
        if (!handle.IsValid || handle.Index >= _highWaterMark)
            return false;

        ref readonly Slot slot = ref _slots[handle.Index];
        return slot.IsAlive && slot.Generation == handle.Generation;
    }

    public ref T Get(ResourceHandle handle)
    {
#if GRAPHICS_DEBUG
        if (!IsValid(handle))
            throw new InvalidOperationException($"Attempted to access invalid or destroyed handle ({handle.Index}:{handle.Generation}).");
#endif
        return ref _slots[handle.Index].Value;
    }

    public bool TryRemove(ResourceHandle handle, out T value)
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
        {
            slot.Generation = 1; // Skip 0 on wrap-around
        }

        _freeIndices.Push(handle.Index);
        LiveCount--;
        return true;
    }

    /// <summary>
    /// Drains all remaining live entries (used during shutdown cleanup and leak reporting).
    /// </summary>
    public void DrainAll(Action<ResourceHandle, T> onItem)
    {
        for (uint i = 0; i < _highWaterMark; i++)
        {
            ref Slot slot = ref _slots[i];
            if (!slot.IsAlive)
                continue;

            var handle = new ResourceHandle(i, slot.Generation);
            T value = slot.Value;
            slot.Value = default;
            slot.IsAlive = false;
            slot.Generation++;
            LiveCount--;

            onItem(handle, value);
        }

        _freeIndices.Clear();
        _highWaterMark = 0;
    }
}