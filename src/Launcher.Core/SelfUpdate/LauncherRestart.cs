using System.Diagnostics;

namespace Launcher.Core.SelfUpdate;

/// <summary>Starts the freshly swapped-in launcher and exits the current process.</summary>
public static class LauncherRestart
{
    public static void RestartTo(string exePath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = OperatingSystem.IsWindows(),
        };

        Process.Start(startInfo);
        Environment.Exit(0);
    }
}
