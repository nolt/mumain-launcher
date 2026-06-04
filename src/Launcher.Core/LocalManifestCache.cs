using System.Text.Json;

namespace Launcher.Core;

/// <summary>
/// Remembers the hash of each local file together with the size and last-write
/// time it had when hashed. If those still match, the cached hash can be trusted
/// without re-reading the whole file - the slow part when checking 700+ MB.
///
/// The cache is an optimization only: a missing, stale or corrupt entry simply
/// causes the file to be hashed again, so it never affects correctness.
/// </summary>
public sealed class LocalManifestCache
{
    /// <summary>One cached file: the hash, plus the size and timestamp it was hashed at.</summary>
    public sealed record Entry(long Size, DateTime LastWriteUtc, string Hash);

    private const string CacheFileName = "local-state.json";

    private readonly string _cacheFilePath;
    private readonly Dictionary<string, Entry> _entries;

    private LocalManifestCache(string cacheFilePath, Dictionary<string, Entry> entries)
    {
        _cacheFilePath = cacheFilePath;
        _entries = entries;
    }

    public static LocalManifestCache Load(string clientDirectory)
    {
        var path = Path.Combine(clientDirectory, CacheFileName);
        return new LocalManifestCache(path, ReadEntries(path));
    }

    public bool TryGet(string relativePath, out Entry entry) =>
        _entries.TryGetValue(relativePath, out entry!);

    public void Set(string relativePath, Entry entry) => _entries[relativePath] = entry;

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(_entries);
        await File.WriteAllTextAsync(_cacheFilePath, json, cancellationToken);
    }

    private static Dictionary<string, Entry> ReadEntries(string path)
    {
        if (!File.Exists(path))
        {
            return Empty();
        }

        try
        {
            var json = File.ReadAllText(path);
            var entries = JsonSerializer.Deserialize<Dictionary<string, Entry>>(json);
            return entries is null ? Empty() : new Dictionary<string, Entry>(entries, StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            // A corrupt cache is harmless: treat it as empty and rebuild.
            return Empty();
        }
    }

    private static Dictionary<string, Entry> Empty() => new(StringComparer.Ordinal);
}
