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
        var command = ClientLaunchCommand.Create(_clientDirectory, _executableName, OperatingSystem.IsWindows());
        var startInfo = new ProcessStartInfo
        {
            FileName = command.FileName,
            WorkingDirectory = command.WorkingDirectory,
            UseShellExecute = OperatingSystem.IsWindows(),
        };

        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            Process.Start(startInfo);
        }
        catch (Win32Exception ex)
        {
            throw new ClientLaunchException(DescribeFailure(command), ex);
        }
    }

    private static string DescribeFailure(ClientLaunchCommand command)
    {
        if (command.FileName == ClientLaunchCommand.WineExecutable)
        {
            return "Could not start the client through Wine. Is Wine installed?";
        }

        return $"Could not start the client: {command.FileName}";
    }
}
