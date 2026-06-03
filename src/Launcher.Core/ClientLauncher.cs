using System.ComponentModel;
using System.Diagnostics;

namespace Launcher.Core;

/// <summary>Starts the game client, choosing the right command for the current OS.</summary>
public sealed class ClientLauncher
{
    private readonly string _clientDirectory;
    private readonly string _executableName;

    public ClientLauncher(string clientDirectory, string executableName)
    {
        _clientDirectory = clientDirectory;
        _executableName = executableName;
    }

    public void Launch()
    {
        var isWindows = OperatingSystem.IsWindows();
        var linuxConfig = isWindows ? null : LinuxLaunchConfig.Load(_clientDirectory);
        var command = ClientLaunchCommand.Create(_clientDirectory, _executableName, isWindows, linuxConfig);

        var startInfo = new ProcessStartInfo
        {
            FileName = command.FileName,
            WorkingDirectory = command.WorkingDirectory,
            UseShellExecute = isWindows,
        };

        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var (key, value) in command.EnvironmentOverrides)
        {
            startInfo.Environment[key] = value;
        }

        try
        {
            Process.Start(startInfo);
        }
        catch (Win32Exception ex)
        {
            throw new ClientLaunchException(DescribeFailure(command.FileName, isWindows), ex);
        }
    }

    private static string DescribeFailure(string fileName, bool isWindows)
    {
        if (!isWindows)
        {
            return $"Could not start the client through '{fileName}'. "
                + $"Is Wine installed, or set wineCommand in {LinuxLaunchConfig.FileName}?";
        }

        return $"Could not start the client: {fileName}";
    }
}
