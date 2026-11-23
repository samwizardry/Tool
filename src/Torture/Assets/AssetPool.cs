using Torture.Collections;
using Torture.Logging;

namespace Torture.Assets;

/// <summary>
/// Reference-counted generational storage for assets of type <typeparamref name="T"/>,
/// supporting optional deduplication by <see cref="AssetId"/>.
/// </summary>
internal sealed class AssetPool<T>
    where T : struct
{
    private struct Entry
    {
        public AssetId Id;
        public T Asset;
        public int RefCount;
    }

    private readonly SlotMap<Entry> _slots = new();
    private readonly Dictionary<AssetId, AssetHandle<T>> _byId = [];

    public int LiveCount => _slots.LiveCount;

    public bool TryAcquire(AssetId id, out AssetHandle<T> handle)
    {
        if (id.IsValid && _byId.TryGetValue(id, out handle))
        {
            ref Entry entry = ref _slots.Get(handle.Raw);
            entry.RefCount++;
            return true;
        }

        handle = AssetHandle<T>.Invalid;
        return false;
    }

    public AssetHandle<T> Add(AssetId id, in T asset)
    {
        SlotHandle raw = _slots.Add(new Entry
        {
            Id = id,
            Asset = asset,
            RefCount = 1
        });

        var handle = new AssetHandle<T>(raw);
        if (id.IsValid)
        {
            _byId[id] = handle;
        }

        return handle;
    }

    public void AddRef(AssetHandle<T> handle)
    {
        ref Entry entry = ref _slots.Get(handle.Raw);
        entry.RefCount++;
    }

    public bool IsValid(AssetHandle<T> handle) => _slots.IsValid(handle.Raw);

    public ref T Get(AssetHandle<T> handle) => ref _slots.Get(handle.Raw).Asset;

    public bool TryReleaseRef(AssetHandle<T> handle, out T removedAsset)
    {
        if (!_slots.IsValid(handle.Raw))
        {
            Log.Warning(LogCategories.Assets, $"Attempted to release invalid AssetHandle<{typeof(T).Name}> ({handle.Index}:{handle.Generation}).");
            removedAsset = default;
            return false;
        }

        ref Entry entry = ref _slots.Get(handle.Raw);
        entry.RefCount--;

        if (entry.RefCount > 0)
        {
            removedAsset = default;
            return false;
        }

        AssetId id = entry.Id;
        if (id.IsValid)
        {
            _byId.Remove(id);
        }

        _slots.TryRemove(handle.Raw, out Entry removedEntry);
        removedAsset = removedEntry.Asset;
        return true;
    }

    public void DrainAll(Action<AssetId, AssetHandle<T>, T> onItem)
    {
        _byId.Clear();
        _slots.DrainAll((raw, entry) => onItem(entry.Id, new AssetHandle<T>(raw), entry.Asset));
    }
}
