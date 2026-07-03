namespace Launcher.Core;

/// <summary>
/// Orchestrates an update: fetch the manifest, work out which files are missing
/// or out of date, download them with verification, and persist the local cache.
/// This is the single entry point the UI calls.
/// </summary>
public sealed class ClientUpdater
{
    private readonly IPatchSource _source;
    private readonly string _clientDirectory;
    private readonly string? _executableToMark;

    /// <param name="executableToMark">
    /// Relative path of a client executable to mark executable (chmod +x) after the
    /// update, for the native Linux client whose downloaded <c>Main</c> ELF would
    /// otherwise land without the exec bit. Null (Windows / Wine) skips this.
    /// </param>
    public ClientUpdater(IPatchSource source, string clientDirectory, string? executableToMark = null)
    {
        _source = source;
        _clientDirectory = clientDirectory;
        _executableToMark = executableToMark;
    }

    public async Task<UpdateResult> UpdateAsync(IProgress<UpdateProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report(new UpdateProgress(UpdatePhase.FetchingManifest, null, 0, 0, 0, 0));
        var manifest = await _source.GetManifestAsync(cancellationToken);

        var cache = LocalManifestCache.Load(_clientDirectory);
        var plan = await BuildPlanAsync(manifest, cache, progress, cancellationToken);

        await DownloadAsync(plan, progress, cancellationToken);
        await cache.SaveAsync(cancellationToken);
        MarkExecutable();

        progress?.Report(new UpdateProgress(
            UpdatePhase.Completed, null,
            plan.FilesToDownload.Count, plan.FilesToDownload.Count,
            plan.TotalBytes, plan.TotalBytes));

        return new UpdateResult(manifest.Version, plan.FilesToDownload.Count);
    }

    // The native Linux Main is downloaded as a plain file, so it arrives without the
    // exec bit; set 0755 (mirrors the launcher self-update) so it can be started. No-op
    // on Windows/Wine (null target), and File.SetUnixFileMode is unsupported there.
    private void MarkExecutable()
    {
        if (_executableToMark is null || OperatingSystem.IsWindows())
        {
            return;
        }

        var path = Path.Combine(_clientDirectory, _executableToMark.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            return;
        }

        File.SetUnixFileMode(
            path,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
    }

    private async Task<UpdatePlan> BuildPlanAsync(Manifest manifest, LocalManifestCache cache, IProgress<UpdateProgress>? progress, CancellationToken cancellationToken)
    {
        var comparer = new LocalFileComparer(_clientDirectory, cache);
        var toDownload = new List<ManifestFile>();
        long totalBytes = 0;
        var checkedCount = 0;

        foreach (var file in manifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await comparer.NeedsDownloadAsync(file, cancellationToken))
            {
                toDownload.Add(file);
                totalBytes += file.Size;
            }

            checkedCount++;
            progress?.Report(new UpdateProgress(UpdatePhase.CheckingFiles, file.Path, checkedCount, manifest.Files.Count, 0, 0));
        }

        return new UpdatePlan(toDownload, totalBytes);
    }

    private async Task DownloadAsync(UpdatePlan plan, IProgress<UpdateProgress>? progress, CancellationToken cancellationToken)
    {
        var downloader = new FileDownloader(_source, _clientDirectory);
        long completedBytes = 0;
        var completedFiles = 0;

        foreach (var file in plan.FilesToDownload)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytesBeforeThisFile = completedBytes;
            var fileProgress = new Progress<long>(current => progress?.Report(new UpdateProgress(
                UpdatePhase.Downloading, file.Path,
                completedFiles, plan.FilesToDownload.Count,
                bytesBeforeThisFile + current, plan.TotalBytes)));

            await downloader.DownloadAsync(file, fileProgress, cancellationToken);

            completedBytes += file.Size;
            completedFiles++;
            progress?.Report(new UpdateProgress(
                UpdatePhase.Downloading, file.Path,
                completedFiles, plan.FilesToDownload.Count,
                completedBytes, plan.TotalBytes));
        }
    }
}
