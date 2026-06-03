using System.Security.Cryptography;

namespace Launcher.Core;

/// <summary>
/// Computes the content hash used both to build a manifest and to verify
/// downloads. SHA-256 is the single hash algorithm across the whole launcher.
/// </summary>
public static class FileHasher
{
    /// <summary>
    /// Computes the lowercase hex SHA-256 of a file, reading it as a stream so
    /// large files don't have to be held in memory.
    /// </summary>
    public static async Task<string> ComputeHexAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hash);
    }
}
