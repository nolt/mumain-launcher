namespace Launcher.Core;

/// <summary>
/// One file entry in a patch manifest: the client-relative path (with '/'
/// separators), the lowercase hex SHA-256 of its contents, and its size in bytes.
/// </summary>
public sealed record ManifestFile(string Path, string Hash, long Size);
