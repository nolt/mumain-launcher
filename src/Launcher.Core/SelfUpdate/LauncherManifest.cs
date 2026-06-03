namespace Launcher.Core.SelfUpdate;

/// <summary>One launcher binary in <see cref="LauncherManifest"/>: where it lives and its hash.</summary>
public sealed record LauncherFile(string Path, string Hash, long Size);

/// <summary>
/// Describes the latest launcher build (<c>launcher.json</c>): a version and one
/// binary per runtime identifier, e.g. "win-x64" and "linux-x64".
/// </summary>
public sealed record LauncherManifest(string Version, Dictionary<string, LauncherFile> Files);
