using System.Security.Cryptography;
using System.Text;
using Launcher.Core;
using Xunit;

namespace Launcher.Core.Tests;

public class LocalFileComparerTests : IDisposable
{
    private readonly string _clientDir = Directory.CreateTempSubdirectory("comparer-test-").FullName;

    public void Dispose() => Directory.Delete(_clientDir, recursive: true);

    [Fact]
    public async Task NeedsDownload_TrueWhenFileMissing()
    {
        var comparer = NewComparer();
        var file = new ManifestFile("missing.dat", "anyhash", 10);

        Assert.True(await comparer.NeedsDownloadAsync(file));
    }

    [Fact]
    public async Task NeedsDownload_FalseWhenContentMatches()
    {
        var bytes = Bytes("same");
        WriteLocal("file.dat", bytes);
        var comparer = NewComparer();
        var file = new ManifestFile("file.dat", Hash(bytes), bytes.Length);

        Assert.False(await comparer.NeedsDownloadAsync(file));
    }

    [Fact]
    public async Task NeedsDownload_TrueWhenSizeDiffers()
    {
        WriteLocal("file.dat", Bytes("short"));
        var comparer = NewComparer();
        var file = new ManifestFile("file.dat", "anyhash", 9999);

        Assert.True(await comparer.NeedsDownloadAsync(file));
    }

    [Fact]
    public async Task NeedsDownload_TrueWhenContentChangedAtSameSize()
    {
        WriteLocal("file.dat", Bytes("aaaa"));
        var comparer = NewComparer();
        var file = new ManifestFile("file.dat", Hash(Bytes("bbbb")), 4);

        Assert.True(await comparer.NeedsDownloadAsync(file));
    }

    [Fact]
    public async Task NeedsDownload_TrustsCachedHashWhenSizeAndTimestampUnchanged()
    {
        var info = WriteLocal("file.dat", Bytes("real content"));
        var cache = LocalManifestCache.Load(_clientDir);
        // Cache says this exact (size, mtime) hashes to "cachedhash" — not the real content hash.
        cache.Set("file.dat", new LocalManifestCache.Entry(info.Length, info.LastWriteTimeUtc, "cachedhash"));
        var comparer = new LocalFileComparer(_clientDir, cache);
        var file = new ManifestFile("file.dat", "cachedhash", info.Length);

        // It must match the cached hash without re-reading the file's real content.
        Assert.False(await comparer.NeedsDownloadAsync(file));
    }

    [Fact]
    public async Task NeedsDownload_RehashesWhenTimestampChanged()
    {
        var bytes = Bytes("real content");
        var info = WriteLocal("file.dat", bytes);
        var cache = LocalManifestCache.Load(_clientDir);
        cache.Set("file.dat", new LocalManifestCache.Entry(info.Length, info.LastWriteTimeUtc, "stalehash"));

        // Touch the file: same size, newer timestamp — the cached hash must be discarded.
        File.SetLastWriteTimeUtc(Path.Combine(_clientDir, "file.dat"), info.LastWriteTimeUtc.AddMinutes(5));
        var comparer = new LocalFileComparer(_clientDir, cache);
        var file = new ManifestFile("file.dat", Hash(bytes), bytes.Length);

        Assert.False(await comparer.NeedsDownloadAsync(file));
        Assert.True(cache.TryGet("file.dat", out var entry));
        Assert.Equal(Hash(bytes), entry.Hash);
    }

    private LocalFileComparer NewComparer() => new(_clientDir, LocalManifestCache.Load(_clientDir));

    private FileInfo WriteLocal(string relativePath, byte[] bytes)
    {
        var full = Path.Combine(_clientDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, bytes);
        return new FileInfo(full);
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private static string Hash(byte[] data) => Convert.ToHexStringLower(SHA256.HashData(data));
}
