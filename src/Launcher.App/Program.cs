using System;
using System.Diagnostics;
using Avalonia;

namespace Launcher.App;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // X11 WM_CLASS = the binary's own name (set per build via LAUNCHER_NAME),
            // so GNOME can match the window to its .desktop file. install-linux.sh
            // writes the same name into StartupWMClass - see packaging/linux/.
            .With(new X11PlatformOptions { WmClass = Process.GetCurrentProcess().ProcessName })
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
