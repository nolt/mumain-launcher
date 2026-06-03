namespace Launcher.Core;

/// <summary>
/// Build-time configuration baked into the launcher. The launcher is built per
/// server, so these values are constants; set <see cref="ManifestUrl"/> to the
/// real patch host before building for a given server.
/// </summary>
public static class LauncherConfig
{
    /// <summary>
    /// Full URL of the patch manifest (version.json). Placeholder — replace with
    /// the real patch host before building.
    /// </summary>
    public const string ManifestUrl = "https://patch.example.com/version.json";

    /// <summary>Client executable started after the update completes.</summary>
    public const string ClientExecutableName = "main.exe";

    /// <summary>
    /// Directory that holds the client files. The launcher lives in the client
    /// root, so manifest paths resolve relative to its own location.
    /// </summary>
    public static string ClientDirectory => AppContext.BaseDirectory;
}
