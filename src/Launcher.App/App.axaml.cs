using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Launcher.App.ViewModels;
using Launcher.Core;
using Launcher.Core.SelfUpdate;

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
        var httpClient = new HttpClient();
        var updater = new ClientUpdater(new HttpPatchSource(httpClient, LauncherConfig.ManifestUrl), clientDirectory);
        var launcher = new ClientLauncher(clientDirectory, LauncherConfig.ClientExecutableName);
        var selfUpdater = new LauncherSelfUpdater(
            httpClient,
            LauncherConfig.LauncherManifestUrl,
            LauncherConfig.CurrentLauncherVersion,
            LauncherConfig.CurrentExecutablePath,
            RuntimePlatform.Current());

        var clientConfig = new ClientConfig(clientDirectory);

        var window = new MainWindow();
        var viewModel = new MainWindowViewModel(
            updater, launcher, selfUpdater, clientConfig,
            window.Close,
            () => LauncherRestart.RestartTo(LauncherConfig.CurrentExecutablePath));
        window.DataContext = viewModel;
        window.Opened += async (_, _) => await viewModel.StartAsync();
        window.Closing += (_, _) => viewModel.Cancel();
        return window;
    }
}
