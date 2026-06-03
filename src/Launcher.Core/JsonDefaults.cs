using System.Text.Json;

namespace Launcher.Core;

/// <summary>Shared JSON options so every manifest reads and writes the same way.</summary>
internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
