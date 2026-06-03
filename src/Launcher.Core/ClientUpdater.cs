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

    public ClientUpdater(IPatchSource source, string clientDirectory)
    {
        _source = source;
        _clientDirectory = clientDirectory;
    }

    public async Task<UpdateResult> UpdateAsync(IProgress<UpdateProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report(new UpdateProgress(UpdatePhase.FetchingManifest, null, 0, 0, 0, 0));
        var manifest = await _source.GetManifestAsync(cancellationToken);

        var cache = LocalManifestCache.Load(_clientDirectory);
        var plan = await BuildPlanAsync(manifest, cache, progress, cancellationToken);

        await DownloadAsync(plan, progress, cancellationToken);
        await cache.SaveAsync(cancellationToken);

        progress?.Report(new UpdateProgress(
            UpdatePhase.Completed, null,
            plan.FilesToDownload.Count, plan.FilesToDownload.Count,
            plan.TotalBytes, plan.TotalBytes));

        return new UpdateResult(manifest.Version, plan.FilesToDownload.Count);
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
