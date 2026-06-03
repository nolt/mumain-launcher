using System.Text.Json;

namespace Launcher.Core;

/// <summary>
/// Per-player, per-machine launch settings read from <c>launcher.local.json</c>
/// next to the launcher (Linux only). Everything is optional: when the file is
/// absent the client is started with <c>wine</c> and whatever <c>WINEPREFIX</c>
/// the launcher inherited from its environment. The file is local to the player
/// and is never part of a manifest, so the updater never touches it.
/// </summary>
public sealed class LinuxLaunchConfig
{
    public const string FileName = "launcher.local.json";
    public const string DefaultWineCommand = "wine";

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
            return JsonSerializer.Deserialize<LinuxLaunchConfig>(File.ReadAllText(path), JsonDefaults.Options)
                ?? new LinuxLaunchConfig();
        }
        catch (JsonException)
        {
            // A malformed local config falls back to defaults rather than blocking play.
            return new LinuxLaunchConfig();
        }
    }
}
