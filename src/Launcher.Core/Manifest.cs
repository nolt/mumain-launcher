namespace Launcher.Core;

/// <summary>
/// Describes a complete client release: every file a launcher must have, each
/// with the hash it should match. <see cref="Files"/> is listed in stable path
/// order so the same release always serializes identically.
/// </summary>
/// <param name="Version">Human-readable release tag, e.g. a build date "2026.06.03".</param>
/// <param name="GeneratedAtUtc">When the manifest was produced (UTC).</param>
/// <param name="BaseUrl">Location of the files relative to the manifest, e.g. "files/".</param>
/// <param name="Files">The files that make up the release.</param>
public sealed record Manifest(
    string Version,
    DateTime GeneratedAtUtc,
    string BaseUrl,
    IReadOnlyList<ManifestFile> Files);
