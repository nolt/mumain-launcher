namespace PatchManifest;

/// <summary>Resolved command-line options for the manifest generator.</summary>
public sealed record CommandLineOptions
{
    /// <summary>Absolute path of the release directory to scan.</summary>
    public required string InputDirectory { get; init; }

    /// <summary>Absolute path of the manifest file to write.</summary>
    public required string OutputFile { get; init; }

    /// <summary>Value written to the manifest's <c>baseUrl</c> field.</summary>
    public required string BaseUrl { get; init; }

    /// <summary>Value written to the manifest's <c>version</c> field.</summary>
    public required string Version { get; init; }

    /// <summary>File-name globs to skip (e.g. <c>config.ini</c>, <c>*.log</c>).</summary>
    public required IReadOnlyList<string> ExcludeGlobs { get; init; }
}
