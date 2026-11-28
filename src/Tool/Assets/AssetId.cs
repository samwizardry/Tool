using System.Security.Cryptography;
using System.Text;

namespace Tool.Assets;

/// <summary>
/// Persistent, globally unique asset identifier backed by a 128-bit GUID.
/// </summary>
public readonly record struct AssetId
{
    public readonly Guid Id;

    public AssetId(Guid id)
    {
        Id = id;
    }

    public static AssetId Empty => default;

    public bool IsValid => Id != Guid.Empty;

    public static AssetId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a deterministic identifier from a normalized asset key or virtual path.
    /// </summary>
    public static AssetId FromPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string normalized = NormalizePath(path);
        int maxByteCount = Encoding.UTF8.GetMaxByteCount(normalized.Length);
        Span<byte> utf8 = maxByteCount <= 512 ? stackalloc byte[maxByteCount] : new byte[maxByteCount];
        int bytesWritten = Encoding.UTF8.GetBytes(normalized, utf8);

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(utf8[..bytesWritten], hash);

        return new AssetId(new Guid(hash[..16]));
    }

    public static string NormalizePath(string path) =>
        path.Trim().Replace('\\', '/').ToLowerInvariant();

    public override string ToString() => Id.ToString("N");
}
