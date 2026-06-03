using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Launcher.App.ViewModels;
using Launcher.Core;

namespace Launcher.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = CreateMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainWindow CreateMainWindow()
    {
        var clientDirectory = LauncherConfig.ClientDirectory;
        var source = new HttpPatchSource(new HttpClient(), LauncherConfig.ManifestUrl);
        var updater = new ClientUpdater(source, clientDirectory);
        var launcher = new ClientLauncher(clientDirectory, LauncherConfig.ClientExecutableName);

        var window = new MainWindow();
        var viewModel = new MainWindowViewModel(updater, launcher, window.Close);
        window.DataContext = viewModel;
        window.Opened += async (_, _) => await viewModel.StartAsync();
        window.Closing += (_, _) => viewModel.Cancel();
        return window;
    }
}
