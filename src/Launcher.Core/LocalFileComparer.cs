namespace Launcher.Core;

/// <summary>
/// Decides, for one manifest file, whether the local copy must be (re)downloaded.
/// Uses <see cref="LocalManifestCache"/> to avoid re-hashing files whose size and
/// timestamp are unchanged since they were last hashed.
/// </summary>
public sealed class LocalFileComparer
{
    private readonly string _clientDirectory;
    private readonly LocalManifestCache _cache;

    public LocalFileComparer(string clientDirectory, LocalManifestCache cache)
    {
        _clientDirectory = clientDirectory;
        _cache = cache;
    }

    public async Task<bool> NeedsDownloadAsync(ManifestFile file, CancellationToken cancellationToken = default)
    {
        var localPath = ToLocalPath(file.Path);
        var info = new FileInfo(localPath);
        if (!info.Exists)
        {
            return true;
        }

        // A size mismatch is conclusive on its own — no need to read the file.
        if (info.Length != file.Size)
        {
            return true;
        }

        var localHash = await GetLocalHashAsync(localPath, info, file.Path, cancellationToken);
        return !string.Equals(localHash, file.Hash, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> GetLocalHashAsync(string localPath, FileInfo info, string relativePath, CancellationToken cancellationToken)
    {
        if (_cache.TryGet(relativePath, out var entry)
            && entry.Size == info.Length
            && entry.LastWriteUtc == info.LastWriteTimeUtc)
        {
            return entry.Hash;
        }

        var hash = await FileHasher.ComputeHexAsync(localPath, cancellationToken);
        _cache.Set(relativePath, new LocalManifestCache.Entry(info.Length, info.LastWriteTimeUtc, hash));
        return hash;
    }

    private string ToLocalPath(string relativePath) =>
        Path.Combine(_clientDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
}
