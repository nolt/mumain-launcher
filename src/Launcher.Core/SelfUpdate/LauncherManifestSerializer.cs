using System.Text.Json;

namespace Launcher.Core.SelfUpdate;

/// <summary>Reads and writes <see cref="LauncherManifest"/> as JSON.</summary>
public static class LauncherManifestSerializer
{
    public static string Serialize(LauncherManifest manifest) =>
        JsonSerializer.Serialize(manifest, JsonDefaults.Options);

    public static LauncherManifest? Deserialize(string json) =>
        JsonSerializer.Deserialize<LauncherManifest>(json, JsonDefaults.Options);
}
