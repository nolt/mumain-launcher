namespace Launcher.Core;

/// <summary>The set of files that must be downloaded to bring the client up to date.</summary>
public sealed record UpdatePlan(IReadOnlyList<ManifestFile> FilesToDownload, long TotalBytes)
{
    public bool IsUpToDate => FilesToDownload.Count == 0;
}
