using Torture.Collections;

namespace Torture.Assets;

/// <summary>
/// Lightweight, unmanaged generational handle referencing an asset stored inside <see cref="AssetManager"/>.
/// </summary>
public readonly record struct AssetHandle<T>(SlotHandle Raw)
    where T : struct
{
    public static AssetHandle<T> Invalid => default;

    public bool IsValid => Raw.IsValid;
    public uint Index => Raw.Index;
    public uint Generation => Raw.Generation;
}
