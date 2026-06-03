namespace Launcher.Core;

/// <summary>Outcome of an update run.</summary>
/// <param name="Version">The version reported by the manifest that was applied.</param>
/// <param name="UpdatedFileCount">How many files were downloaded.</param>
public sealed record UpdateResult(string Version, int UpdatedFileCount)
{
    public bool WasUpToDate => UpdatedFileCount == 0;
}
