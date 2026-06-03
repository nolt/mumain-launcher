using System.Text.Json;

namespace Launcher.Core;

/// <summary>
/// Reads and writes <see cref="Manifest"/> as JSON, so the generator and the
/// launcher always agree on the wire format.
/// </summary>
public static class ManifestSerializer
{
    public static string Serialize(Manifest manifest) =>
        JsonSerializer.Serialize(manifest, JsonDefaults.Options);

    public static Manifest? Deserialize(string json) =>
        JsonSerializer.Deserialize<Manifest>(json, JsonDefaults.Options);
}
