namespace Launcher.Core.SelfUpdate;

/// <summary>
/// Maps the current OS to the runtime identifier used as a key in
/// <see cref="LauncherManifest.Files"/>. Only x64 builds are produced.
/// </summary>
public static class RuntimePlatform
{
    public const string Windows64 = "win-x64";
    public const string Linux64 = "linux-x64";

    public static string RidFor(bool isWindows) => isWindows ? Windows64 : Linux64;

    public static string Current() => RidFor(OperatingSystem.IsWindows());
}
