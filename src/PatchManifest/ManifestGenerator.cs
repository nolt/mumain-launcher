using System.IO.Enumeration;
using Launcher.Core;

namespace PatchManifest;

/// <summary>
/// Scans a release directory and produces a <see cref="Manifest"/>: one entry
/// per included file, with its relative path, SHA-256 hash and size.
/// </summary>
public sealed class ManifestGenerator
{
    private readonly CommandLineOptions _options;

    public ManifestGenerator(CommandLineOptions options) => _options = options;

    public async Task<Manifest> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var files = new List<ManifestFile>();
        foreach (var filePath in EnumerateIncludedFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = ToRelativeUrlPath(filePath);
            var hash = await FileHasher.ComputeHexAsync(filePath, cancellationToken);
            var size = new FileInfo(filePath).Length;
            files.Add(new ManifestFile(relativePath, hash, size));
        }

        // Stable ordering keeps the manifest byte-identical for an unchanged release.
        files.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));

        return new Manifest(_options.Version, DateTime.UtcNow, _options.BaseUrl, files);
    }

    private IEnumerable<string> EnumerateIncludedFiles()
    {
        var all = Directory.EnumerateFiles(_options.InputDirectory, "*", SearchOption.AllDirectories);
        foreach (var path in all)
        {
            if (IsExcluded(path))
            {
                continue;
            }

            yield return path;
        }
    }

    private bool IsExcluded(string filePath)
    {
        // Never include the manifest we are about to write, even if it sits inside the input.
        if (PathsEqual(filePath, _options.OutputFile))
        {
            return true;
        }

        var fileName = Path.GetFileName(filePath);
        foreach (var glob in _options.ExcludeGlobs)
        {
            if (FileSystemName.MatchesSimpleExpression(glob, fileName, ignoreCase: true))
            {
                return true;
            }
        }

        return false;
    }

    private string ToRelativeUrlPath(string filePath)
    {
        var relative = Path.GetRelativePath(_options.InputDirectory, filePath);
        return relative.Replace('\\', '/');
    }

    private static bool PathsEqual(string a, string b)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), comparison);
    }
}
