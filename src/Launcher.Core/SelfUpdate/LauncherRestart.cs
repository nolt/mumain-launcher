using System.Diagnostics;

namespace Launcher.Core.SelfUpdate;

/// <summary>Starts the freshly swapped-in launcher and exits the current process.</summary>
public static class LauncherRestart
{
    public static void RestartTo(string exePath)
    {
        try
        {
            Process.Start(BuildStartInfo(exePath));
        }
        catch (Exception ex)
        {
            TryLogFailure(exePath, ex);
        }

        Environment.Exit(0);
    }

    private static ProcessStartInfo BuildStartInfo(string exePath)
    {
        var workingDirectory = Path.GetDirectoryName(exePath)!;

        if (OperatingSystem.IsWindows())
        {
            // Relaunch through a detached, hidden helper that waits ~1s first, so the
            // current process has fully exited and released the executable file and
            // the single-file extraction directory before the new instance starts.
            // ShellExecute of a just-replaced exe can race or be blocked; this avoids
            // it. `ping -n 2` is a console-free ~1s delay (timeout needs a console).
            return new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c ping -n 2 127.0.0.1 >nul & start \"\" \"{exePath}\"",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
        }

        return new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
        };
    }

    private static void TryLogFailure(string exePath, Exception ex)
    {
        try
        {
            var log = Path.Combine(Path.GetDirectoryName(exePath)!, "launcher-update-error.log");
            File.AppendAllText(log, $"Restart failed: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
        }
        catch
        {
            // Logging is best-effort; never let it mask the original failure.
        }
    }
}
