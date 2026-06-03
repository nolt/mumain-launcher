using System.Security.Cryptography;

namespace Launcher.Core;

/// <summary>
/// Downloads a single file to a temporary path, verifies its SHA-256 against the
/// manifest, then atomically moves it into place. Transient failures are retried;
/// a half-written target file is never left behind.
/// </summary>
public sealed class FileDownloader
{
    private const int MaxAttempts = 3;
    private const string TempSuffix = ".download";
    private const int CopyBufferSize = 81920;

    private readonly IPatchSource _source;
    private readonly string _clientDirectory;

    public FileDownloader(IPatchSource source, string clientDirectory)
    {
        _source = source;
        _clientDirectory = clientDirectory;
    }

    /// <summary>
    /// Downloads <paramref name="file"/>. <paramref name="currentFileBytes"/>, if
    /// supplied, reports the running byte count for this file (it restarts at zero
    /// on each retry).
    /// </summary>
    public async Task DownloadAsync(ManifestFile file, IProgress<long>? currentFileBytes, CancellationToken cancellationToken = default)
    {
        var targetPath = ToLocalPath(file.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        UpdateException? lastError = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await DownloadOnceAsync(file, targetPath, currentFileBytes, cancellationToken);
                return;
            }
            catch (UpdateException ex) when (attempt < MaxAttempts)
            {
                lastError = ex;
            }
        }

        throw lastError ?? new UpdateException($"Failed to download {file.Path}.");
    }

    private async Task DownloadOnceAsync(ManifestFile file, string targetPath, IProgress<long>? currentFileBytes, CancellationToken cancellationToken)
    {
        var tempPath = targetPath + TempSuffix;
        try
        {
            var hash = await WriteToTempAsync(file, tempPath, currentFileBytes, cancellationToken);
            if (!string.Equals(hash, file.Hash, StringComparison.OrdinalIgnoreCase))
            {
                throw new UpdateException($"Hash mismatch for {file.Path} after download.");
            }

            File.Move(tempPath, targetPath, overwrite: true);
        }
        finally
        {
            DeleteIfExists(tempPath);
        }
    }

    private async Task<string> WriteToTempAsync(ManifestFile file, string tempPath, IProgress<long>? currentFileBytes, CancellationToken cancellationToken)
    {
        await using var remote = await _source.OpenFileAsync(file, cancellationToken);
        await using var temp = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize, useAsync: true);
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        var buffer = new byte[CopyBufferSize];
        long writtenSoFar = 0;
        int read;
        while ((read = await remote.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await temp.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            hasher.AppendData(buffer, 0, read);
            writtenSoFar += read;
            currentFileBytes?.Report(writtenSoFar);
        }

        return Convert.ToHexStringLower(hasher.GetHashAndReset());
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string ToLocalPath(string relativePath) =>
        Path.Combine(_clientDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
}
