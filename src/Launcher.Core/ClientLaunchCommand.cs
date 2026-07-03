namespace Launcher.Core;

/// <summary>
/// The OS-specific command used to start the client. A pure record with a pure
/// factory, so the platform branching can be unit-tested without starting a process.
/// </summary>
/// <param name="FileName">Executable to run.</param>
/// <param name="Arguments">Arguments passed to it.</param>
/// <param name="WorkingDirectory">Directory the process starts in.</param>
/// <param name="EnvironmentOverrides">Environment variables to set on the child (e.g. WINEPREFIX).</param>
public sealed record ClientLaunchCommand(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> EnvironmentOverrides)
{
    private static readonly IReadOnlyDictionary<string, string> NoEnvironment = new Dictionary<string, string>();

    /// <summary>
    /// Builds the launch command. Platform-first: on Windows the client always runs
    /// directly and no native/wine detection happens. On Linux the choice comes from
    /// <paramref name="linuxConfig"/>'s <see cref="LinuxLaunchConfig.Mode"/>: a native
    /// client (<c>Main</c>) runs directly like on Windows, while Wine (the default when
    /// unset, for backward compatibility) runs the Windows client through the configured
    /// Wine binary/prefix. The executable name is supplied by the caller to match the
    /// mode (<c>Main</c> for native, <c>Main.exe</c> for Wine) and is resolved against
    /// the working directory, so nothing depends on an absolute path.
    /// </summary>
    public static ClientLaunchCommand Create(string clientDirectory, string executableName, bool isWindows, LinuxLaunchConfig? linuxConfig = null)
    {
        if (isWindows)
        {
            var executablePath = Path.Combine(clientDirectory, executableName);
            return new ClientLaunchCommand(executablePath, [], clientDirectory, NoEnvironment);
        }

        var config = linuxConfig ?? new LinuxLaunchConfig();
        if (config.Mode == ClientMode.Native)
        {
            // Native Linux client: run the ELF directly, exactly like the Windows path
            // (no Wine). The updater marks it executable after download.
            var nativePath = Path.Combine(clientDirectory, executableName);
            return new ClientLaunchCommand(nativePath, [], clientDirectory, NoEnvironment);
        }

        var environment = config.WinePrefix is null
            ? NoEnvironment
            : new Dictionary<string, string> { ["WINEPREFIX"] = config.WinePrefix };

        return new ClientLaunchCommand(config.WineCommand, [executableName], clientDirectory, environment);
    }
}
