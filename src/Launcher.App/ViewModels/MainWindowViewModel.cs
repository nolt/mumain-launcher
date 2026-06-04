using System.Globalization;
using Avalonia;
using Launcher.Core;
using Launcher.Core.SelfUpdate;

namespace Launcher.App.ViewModels;

/// <summary>
/// Drives the launcher window: runs the update on open, maps progress to the
/// status text and progress bar, and starts the client when the user clicks Play.
/// Holds no update logic of its own - it delegates to <see cref="ClientUpdater"/>.
/// User-facing text comes from the branding resources (see Branding.axaml), so it
/// is translatable per build.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly ClientUpdater _updater;
    private readonly ClientLauncher _launcher;
    private readonly LauncherSelfUpdater _selfUpdater;
    private readonly Action _closeWindow;
    private readonly Action _restart;
    private readonly CancellationTokenSource _cancellation = new();

    private string _statusText = Text("UiStarting", "Starting…");
    private double _progressValue;
    private bool _isProgressIndeterminate = true;
    private bool _canPlay;
    private bool _canRetry;
    private bool _isSettingsOpen;

    public MainWindowViewModel(ClientUpdater updater, ClientLauncher launcher, LauncherSelfUpdater selfUpdater, ClientConfig clientConfig, Action closeWindow, Action restart)
    {
        _updater = updater;
        _launcher = launcher;
        _selfUpdater = selfUpdater;
        _closeWindow = closeWindow;
        _restart = restart;
        PlayCommand = new RelayCommand(Play, () => CanPlay);
        RetryCommand = new RelayCommand(() => _ = RunUpdateAsync(), () => CanRetry);
        Settings = new SettingsViewModel(clientConfig, () => IsSettingsOpen = false);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
    }

    public RelayCommand PlayCommand { get; }

    public RelayCommand RetryCommand { get; }

    public RelayCommand OpenSettingsCommand { get; }

    public SettingsViewModel Settings { get; }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        private set => SetField(ref _isSettingsOpen, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetField(ref _progressValue, value);
    }

    public bool IsProgressIndeterminate
    {
        get => _isProgressIndeterminate;
        private set => SetField(ref _isProgressIndeterminate, value);
    }

    public bool CanPlay
    {
        get => _canPlay;
        private set
        {
            if (SetField(ref _canPlay, value))
            {
                PlayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanRetry
    {
        get => _canRetry;
        private set
        {
            if (SetField(ref _canRetry, value))
            {
                RetryCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Begins the update. Called once, when the window is shown.</summary>
    public Task StartAsync() => RunUpdateAsync();

    /// <summary>Cancels an in-flight update, e.g. when the window is closing.</summary>
    public void Cancel() => _cancellation.Cancel();

    private async Task RunUpdateAsync()
    {
        CanPlay = false;
        CanRetry = false;
        IsProgressIndeterminate = true;
        StatusText = Text("UiCheckingUpdates", "Checking for updates…");

        var progress = new Progress<UpdateProgress>(OnProgress);
        try
        {
            StatusText = Text("UiUpdatingLauncher", "Updating launcher…");
            if (await _selfUpdater.TryUpdateAsync(_cancellation.Token))
            {
                _restart();
                return;
            }

            var result = await _updater.UpdateAsync(progress, _cancellation.Token);
            IsProgressIndeterminate = false;
            ProgressValue = 100;
            StatusText = result.WasUpToDate
                ? Text("UiUpToDate", "Up to date — ready to play.")
                : Format("UiUpdated", "Updated to {0} — ready to play.", result.Version);
            CanPlay = true;
        }
        catch (OperationCanceledException)
        {
            // The window is closing; no message needed.
        }
        catch (UpdateException)
        {
            IsProgressIndeterminate = false;
            StatusText = Text("UiUpdateError", "Could not update. Check your connection and try again.");
            CanRetry = true;
        }
    }

    private void OnProgress(UpdateProgress progress)
    {
        switch (progress.Phase)
        {
            case UpdatePhase.FetchingManifest:
                IsProgressIndeterminate = true;
                StatusText = Text("UiContactingServer", "Contacting update server…");
                break;

            case UpdatePhase.CheckingFiles:
                IsProgressIndeterminate = true;
                StatusText = Format("UiCheckingFiles", "Checking files… {0}/{1}",
                    progress.FilesCompleted, progress.FilesTotal);
                break;

            case UpdatePhase.Downloading:
                IsProgressIndeterminate = false;
                ProgressValue = progress.BytesTotal == 0
                    ? 0
                    : (double)progress.BytesCompleted / progress.BytesTotal * 100;
                StatusText = Format("UiDownloading", "Downloading {0}/{1}  ({2} / {3})",
                    progress.FilesCompleted, progress.FilesTotal,
                    ByteSizeFormatter.Format(progress.BytesCompleted),
                    ByteSizeFormatter.Format(progress.BytesTotal));
                break;

            case UpdatePhase.Completed:
                ProgressValue = 100;
                break;
        }
    }

    private void OpenSettings()
    {
        Settings.Load();
        IsSettingsOpen = true;
    }

    private void Play()
    {
        try
        {
            _launcher.Launch();
            _closeWindow();
        }
        catch (ClientLaunchException)
        {
            StatusText = Text("UiLaunchError", "Could not start the client. On Linux, is Wine installed?");
        }
    }

    /// <summary>Reads a UI string from the branding resources, falling back to English.</summary>
    private static string Text(string key, string fallback) =>
        Application.Current is { } app && app.TryGetResource(key, null, out var value) && value is string s
            ? s
            : fallback;

    private static string Format(string key, string fallback, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, Text(key, fallback), args);
}
