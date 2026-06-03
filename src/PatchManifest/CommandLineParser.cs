using System.Globalization;

namespace PatchManifest;

/// <summary>
/// Parses the generator's command line into <see cref="CommandLineOptions"/>,
/// applying defaults. Throws <see cref="ArgumentException"/> on invalid input.
/// </summary>
public static class CommandLineParser
{
    // Empty by default: client files are hosted next to version.json on the server.
    private const string DefaultBaseUrl = "";
    private const string DefaultManifestFileName = "version.json";
    private const string VersionDateFormat = "yyyy.MM.dd";

    /// <summary>
    /// Player-specific files that must never be in the manifest: shipping them
    /// would let the launcher overwrite a player's own settings or runtime files.
    /// </summary>
    private static readonly string[] DefaultExcludeGlobs = ["config.ini", "*.log", "imgui.ini"];

    public static CommandLineOptions Parse(IReadOnlyList<string> args)
    {
        string? input = null;
        string? output = null;
        string baseUrl = DefaultBaseUrl;
        string? version = null;
        var excludes = new List<string>(DefaultExcludeGlobs);

        for (var i = 0; i < args.Count; i++)
        {
            var (key, value) = SplitArgument(args, ref i);
            switch (key)
            {
                case "--input": input = value; break;
                case "--output": output = value; break;
                case "--base-url": baseUrl = value; break;
                case "--version": version = value; break;
                case "--exclude": excludes.Add(value); break;
                default: throw new ArgumentException($"Unknown argument: {key}");
            }
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("--input <dir> is required.");
        }

        var resolvedInput = Path.GetFullPath(input);
        var resolvedOutput = output is null
            ? Path.Combine(resolvedInput, DefaultManifestFileName)
            : Path.GetFullPath(output);

        return new CommandLineOptions
        {
            InputDirectory = resolvedInput,
            OutputFile = resolvedOutput,
            BaseUrl = baseUrl,
            Version = version ?? DateTime.UtcNow.ToString(VersionDateFormat, CultureInfo.InvariantCulture),
            ExcludeGlobs = excludes,
        };
    }

    /// <summary>
    /// Reads one option as either "--key value" or "--key=value", advancing the
    /// index past a consumed value in the first form.
    /// </summary>
    private static (string Key, string Value) SplitArgument(IReadOnlyList<string> args, ref int index)
    {
        var token = args[index];
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Expected an option starting with '--', got: {token}");
        }

        var equals = token.IndexOf('=');
        if (equals >= 0)
        {
            return (token[..equals], token[(equals + 1)..]);
        }

        if (index + 1 >= args.Count)
        {
            throw new ArgumentException($"Missing value for {token}");
        }

        index++;
        return (token, args[index]);
    }
}
