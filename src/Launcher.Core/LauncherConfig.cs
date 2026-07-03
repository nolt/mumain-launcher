using System.Reflection;

namespace Launcher.Core;

/// <summary>
/// Build-time configuration baked into the launcher. The launcher is built per
/// server, so these values are constants; set <see cref="ManifestUrl"/> and
/// <see cref="LauncherManifestUrl"/> to the real patch host before building.
/// </summary>
public static class LauncherConfig
{
    /// <summary>
    /// Full URL of the Windows client's patch manifest (version.json). Also used by
    /// Linux players running the Windows client through Wine. Placeholder - replace
    /// with the real patch host before building.
    /// </summary>
    public const string ManifestUrl = "https://patch.example.com/version.json";

    /// <summary>
    /// Full URL of the native Linux client's patch manifest (version-linux.json). It
    /// lists the native binaries (Main + .so) and shares the asset entries (same
    /// paths/hashes) with <see cref="ManifestUrl"/>, so assets download only once.
    /// </summary>
    public const string LinuxManifestUrl = "https://patch.example.com/version-linux.json";

    /// <summary>Full URL of the launcher self-update manifest (launcher.json).</summary>
    public const string LauncherManifestUrl = "https://patch.example.com/launcher.json";

    /// <summary>
    /// Windows client executable (also the one Wine runs). Case matters: the CMake
    /// target produces "Main.exe" and Linux passes this to a case-sensitive filesystem.
    /// </summary>
    public const string WindowsClientExecutableName = "Main.exe";

    /// <summary>Native Linux client executable - the bare ELF, run directly (no Wine).</summary>
    public const string NativeLinuxClientExecutableName = "Main";

    /// <summary>
    /// Directory that holds the client files. The launcher lives in the client
    /// root, so manifest paths resolve relative to its own location.
    /// </summary>
    public static string ClientDirectory => AppContext.BaseDirectory;

    /// <summary>
    /// This launcher's version, taken from the assembly's informational version
    /// (set via <c>-p:Version=...</c> at publish). Any build metadata after '+' is
    /// dropped so it matches the plain version string in <c>launcher.json</c>.
    /// </summary>
    public static string CurrentLauncherVersion
    {
        get
        {
            var informational = Assembly.GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "0.0.0";
            var plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }
    }

    /// <summary>Absolute path of the running launcher executable.</summary>
    public static string CurrentExecutablePath =>
        Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine the launcher executable path.");
}
