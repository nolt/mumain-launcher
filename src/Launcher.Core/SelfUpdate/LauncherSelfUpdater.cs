using System.IO;
using System.Net.Http;

namespace Launcher.Core.SelfUpdate;

/// <summary>
/// Updates the launcher itself before the client is touched. Best-effort: any
/// failure (no manifest, network error, bad hash) leaves the current launcher in
/// place and lets the client update proceed, since breaking launcher changes are
/// delivered by re-downloading from the website instead.
/// </summary>
public sealed class LauncherSelfUpdater
{
    private const int CopyBufferSize = 81920;

    private readonly HttpClient _httpClient;
    private readonly Uri _manifestUrl;
    private readonly string _currentVersion;
    private readonly string _currentExePath;
    private readonly string _rid;

    public LauncherSelfUpdater(HttpClient httpClient, string manifestUrl, string currentVersion, string currentExePath, string rid)
    {
        _httpClient = httpClient;
        _manifestUrl = new Uri(manifestUrl);
        _currentVersion = currentVersion;
        _currentExePath = currentExePath;
        _rid = rid;
    }

    /// <summary>
    /// Returns <c>true</c> if a newer launcher was downloaded and swapped in - the
    /// caller must then restart. Returns <c>false</c> if already current or if the
    /// check could not be completed.
    /// </summary>
    public async Task<bool> TryUpdateAsync(CancellationToken cancellationToken = default)
    {
        LauncherSwap.CleanupOld(_currentExePath);

        try
        {
            var manifest = await FetchManifestAsync(cancellationToken);
            if (manifest is null || !NeedsUpdate(_currentVersion, manifest))
            {
                return false;
            }

            if (!manifest.Files.TryGetValue(_rid, out var file))
            {
                return false;
            }

            await DownloadVerifiedAsync(file, cancellationToken);
            LauncherSwap.Apply(_currentExePath);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UpdateException)
        {
            DeleteIfExists(LauncherSwap.NewPathFor(_currentExePath));
            return false;
        }
    }

    public static bool NeedsUpdate(string currentVersion, LauncherManifest manifest) =>
        !string.Equals(currentVersion, manifest.Version, StringComparison.Ordinal);

    private async Task<LauncherManifest?> FetchManifestAsync(CancellationToken cancellationToken)
    {
        var json = await _httpClient.GetStringAsync(_manifestUrl, cancellationToken);
        return LauncherManifestSerializer.Deserialize(json);
    }

    private async Task DownloadVerifiedAsync(LauncherFile file, CancellationToken cancellationToken)
    {
        var uri = PatchUris.ResolveSiblingUri(_manifestUrl, file.Path);
        var newPath = LauncherSwap.NewPathFor(_currentExePath);

        using (var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            await using var remote = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var temp = new FileStream(newPath, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize, useAsync: true);
            await remote.CopyToAsync(temp, cancellationToken);
        }

        var hash = await FileHasher.ComputeHexAsync(newPath, cancellationToken);
        if (!string.Equals(hash, file.Hash, StringComparison.OrdinalIgnoreCase))
        {
            throw new UpdateException("Downloaded launcher failed hash verification.");
        }

        MakeExecutableIfNeeded(newPath);
    }

    private static void MakeExecutableIfNeeded(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // A binary fetched over HTTP loses the execute bit; restore it before swapping in.
        File.SetUnixFileMode(
            path,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
