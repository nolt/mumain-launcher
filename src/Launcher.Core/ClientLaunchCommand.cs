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
    /// Builds the launch command. On Windows the client runs directly. Otherwise it
    /// runs through Wine: the executable name is passed as-is and resolved against
    /// the working directory (the client folder), so nothing depends on an absolute
    /// path. An optional <paramref name="linuxConfig"/> overrides the Wine binary
    /// and prefix.
    /// </summary>
    public static ClientLaunchCommand Create(string clientDirectory, string executableName, bool isWindows, LinuxLaunchConfig? linuxConfig = null)
    {
        if (isWindows)
        {
            var executablePath = Path.Combine(clientDirectory, executableName);
            return new ClientLaunchCommand(executablePath, [], clientDirectory, NoEnvironment);
        }

        var config = linuxConfig ?? new LinuxLaunchConfig();
        var environment = config.WinePrefix is null
            ? NoEnvironment
            : new Dictionary<string, string> { ["WINEPREFIX"] = config.WinePrefix };

        return new ClientLaunchCommand(config.WineCommand, [executableName], clientDirectory, environment);
    }
}
