using System.Text.Json;
using System.Text.Json.Serialization;

namespace Launcher.Core;

/// <summary>
/// Per-player, per-machine launch settings read from <c>launcher.local.json</c>
/// next to the launcher (Linux only). Everything is optional: when the file is
/// absent the client is started with <c>wine</c> and whatever <c>WINEPREFIX</c>
/// the launcher inherited from its environment. The file is local to the player
/// and is never part of a manifest, so the updater never touches it.
/// </summary>
public sealed record LinuxLaunchConfig
{
    public const string FileName = "launcher.local.json";
    public const string DefaultWineCommand = "wine";

    // Local to this file so the enum-as-string handling never leaks into the
    // manifest serialization, which must stay byte-stable. A null Mode is omitted
    // on write, so an unset choice leaves no "mode" key in the file.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Which client the player runs (native Linux or Windows-via-Wine). Null means
    /// the choice has not been made yet, so the launcher prompts for it once and
    /// then <see cref="Save"/>s the answer here. Wine is the backward-compatible
    /// default whenever this stays unset.
    /// </summary>
    public ClientMode? Mode { get; init; }

    /// <summary>Wine executable to use (e.g. "wine", "wine64", or a full path).</summary>
    public string WineCommand { get; init; } = DefaultWineCommand;

    /// <summary>Wine prefix to run in. When null, the inherited / default prefix is used.</summary>
    public string? WinePrefix { get; init; }

    public static LinuxLaunchConfig Load(string clientDirectory)
    {
        var path = Path.Combine(clientDirectory, FileName);
        if (!File.Exists(path))
        {
            return new LinuxLaunchConfig();
        }

        try
        {
            return JsonSerializer.Deserialize<LinuxLaunchConfig>(File.ReadAllText(path), JsonOptions)
                ?? new LinuxLaunchConfig();
        }
        catch (JsonException)
        {
            // A malformed local config falls back to defaults rather than blocking play.
            return new LinuxLaunchConfig();
        }
    }

    /// <summary>
    /// Writes this config to <c>launcher.local.json</c> in <paramref name="clientDirectory"/>,
    /// preserving every field (so saving the client-type choice keeps any wine
    /// prefix/command the player already had).
    /// </summary>
    public void Save(string clientDirectory)
    {
        var path = Path.Combine(clientDirectory, FileName);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }
}
