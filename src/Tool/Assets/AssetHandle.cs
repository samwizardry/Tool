using Tool.Collections;

namespace Tool.Assets;

/// <summary>
/// Lightweight, unmanaged generational handle referencing an asset stored inside <see cref="AssetManager"/>.
/// </summary>
public readonly record struct AssetHandle<T>
    where T : struct
{
    public readonly SlotHandle Raw;

    public AssetHandle(SlotHandle raw)
    {
        Raw = raw;
    }

    public static AssetHandle<T> Invalid => default;

    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
