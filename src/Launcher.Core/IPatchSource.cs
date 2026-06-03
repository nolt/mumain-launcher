namespace Launcher.Core;

/// <summary>
/// Source of patch data. Abstracts the network boundary so the update flow can
/// be exercised against a local directory in tests, with HTTP used in production.
/// </summary>
public interface IPatchSource
{
    /// <summary>Downloads and parses the patch manifest.</summary>
    Task<Manifest> GetManifestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a stream over the contents of one file. Must be called after
    /// <see cref="GetManifestAsync"/>. The caller owns and disposes the stream.
    /// </summary>
    Task<Stream> OpenFileAsync(ManifestFile file, CancellationToken cancellationToken = default);
}
