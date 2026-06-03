namespace Launcher.Core;

/// <summary>
/// The OS-specific command used to start the client. A pure record with a pure
/// factory, so the platform branching can be unit-tested without starting a process.
/// </summary>
/// <param name="FileName">Executable to run.</param>
/// <param name="Arguments">Arguments passed to it.</param>
/// <param name="WorkingDirectory">Directory the process starts in.</param>
public sealed record ClientLaunchCommand(string FileName, IReadOnlyList<string> Arguments, string WorkingDirectory)
{
    /// <summary>Executable used to run a Windows client on non-Windows systems.</summary>
    public const string WineExecutable = "wine";

    /// <summary>
    /// Builds the launch command. On Windows the client runs directly; elsewhere
    /// it runs through Wine, since the client is always a Windows binary.
    /// </summary>
    public static ClientLaunchCommand Create(string clientDirectory, string executableName, bool isWindows)
    {
        var executablePath = Path.Combine(clientDirectory, executableName);
        return isWindows
            ? new ClientLaunchCommand(executablePath, [], clientDirectory)
            : new ClientLaunchCommand(WineExecutable, [executablePath], clientDirectory);
    }
}
