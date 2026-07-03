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
        var isWindows = OperatingSystem.IsWindows();

        // The updater and launcher are built per resolved ClientProfile (native vs Wine),
        // which on Linux may not be known until the player is prompted - so they are
        // late-bound through factories rather than constructed up front. The native
        // Linux client's Main needs its exec bit set after download; Windows/Wine don't.
        Func<ClientProfile, ClientUpdater> updaterFactory = profile => new ClientUpdater(
            new HttpPatchSource(httpClient, profile.ManifestUrl),
            clientDirectory,
            executableToMark: !isWindows && profile.ExecutableName == LauncherConfig.NativeLinuxClientExecutableName
                ? profile.ExecutableName
                : null);
        Func<ClientProfile, ClientLauncher> launcherFactory = profile =>
            new ClientLauncher(clientDirectory, profile.ExecutableName);
        IClientModeStore clientModeStore = new LinuxClientModeStore(clientDirectory);

        var selfUpdater = new LauncherSelfUpdater(
            httpClient,
            LauncherConfig.LauncherManifestUrl,
            LauncherConfig.CurrentLauncherVersion,
            LauncherConfig.CurrentExecutablePath,
            RuntimePlatform.Current());

        var clientConfig = new ClientConfig(clientDirectory);

        var window = new MainWindow();
        var viewModel = new MainWindowViewModel(
            updaterFactory, launcherFactory, clientModeStore, isWindows,
            selfUpdater, clientConfig,
            window.Close,
            () => LauncherRestart.RestartTo(LauncherConfig.CurrentExecutablePath));
        window.DataContext = viewModel;
        window.Opened += async (_, _) => await viewModel.StartAsync();
        window.Closing += (_, _) => viewModel.Cancel();
        return window;
    }
}
