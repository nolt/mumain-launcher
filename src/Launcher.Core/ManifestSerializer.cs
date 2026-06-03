using System.Text.Json;

namespace Launcher.Core;

/// <summary>
/// Reads and writes <see cref="Manifest"/> as JSON with one shared option set,
/// so the generator and the launcher always agree on the wire format.
/// </summary>
public static class ManifestSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize(Manifest manifest) =>
        JsonSerializer.Serialize(manifest, Options);

    public static Manifest? Deserialize(string json) =>
        JsonSerializer.Deserialize<Manifest>(json, Options);
}
