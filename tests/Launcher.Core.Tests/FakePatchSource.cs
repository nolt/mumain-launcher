using System.Security.Cryptography;
using Launcher.Core;

namespace Launcher.Core.Tests;

/// <summary>
/// In-memory <see cref="IPatchSource"/> for tests: serves files from a dictionary
/// and derives a manifest from them, so the whole update flow runs without HTTP.
/// </summary>
internal sealed class FakePatchSource : IPatchSource
{
    private readonly Dictionary<string, byte[]> _files;
    private readonly string _version;
    private readonly string _baseUrl;

    public FakePatchSource(Dictionary<string, byte[]> files, string version = "test", string baseUrl = "files/")
    {
        _files = files;
        _version = version;
        _baseUrl = baseUrl;
    }

    /// <summary>How many times a file stream was opened - lets tests assert "nothing was downloaded".</summary>
    public int OpenCount { get; private set; }

    /// <summary>Paths whose served bytes are deliberately corrupted, to trigger hash mismatch.</summary>
    public HashSet<string> CorruptPaths { get; } = new(StringComparer.Ordinal);

    public Task<Manifest> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        var files = _files
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => new ManifestFile(kv.Key, Hash(kv.Value), kv.Value.Length))
            .ToList();
        return Task.FromResult(new Manifest(_version, DateTime.UtcNow, _baseUrl, files));
    }

    public Task<Stream> OpenFileAsync(ManifestFile file, CancellationToken cancellationToken = default)
    {
        OpenCount++;
        var bytes = _files[file.Path];
        if (CorruptPaths.Contains(file.Path))
        {
            bytes = [.. bytes, 0xFF];
        }

        return Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
    }

    public static string Hash(byte[] data) => Convert.ToHexStringLower(SHA256.HashData(data));
}
