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
    private readonly Func<ClientProfile, ClientUpdater> _updaterFactory;
    private readonly Func<ClientProfile, ClientLauncher> _launcherFactory;
    private readonly IClientModeStore _clientModeStore;
    private readonly bool _isWindows;
    private readonly LauncherSelfUpdater _selfUpdater;
    private readonly Action _closeWindow;
    private readonly Action _restart;
    private readonly CancellationTokenSource _cancellation = new();

    private ClientUpdater? _updater;
    private ClientLauncher? _launcher;
    private TaskCompletionSource? _clientChoicePending;

    private string _statusText = Text("UiStarting", "Starting…");
    private double _progressValue;
    private bool _isProgressIndeterminate = true;
    private bool _canPlay;
    private bool _canRetry;
    private bool _isSettingsOpen;
    private bool _isClientChoiceOpen;
    private bool _canCancelClientChoice;

    public MainWindowViewModel(
        Func<ClientProfile, ClientUpdater> updaterFactory,
        Func<ClientProfile, ClientLauncher> launcherFactory,
        IClientModeStore clientModeStore,
        bool isWindows,
        LauncherSelfUpdater selfUpdater,
        ClientConfig clientConfig,
        Action closeWindow,
        Action restart)
    {
        _updaterFactory = updaterFactory;
        _launcherFactory = launcherFactory;
        _clientModeStore = clientModeStore;
        _isWindows = isWindows;
        _selfUpdater = selfUpdater;
        _closeWindow = closeWindow;
        _restart = restart;
        PlayCommand = new RelayCommand(Play, () => CanPlay);
        RetryCommand = new RelayCommand(() => _ = RunUpdateAsync(), () => CanRetry);
        Settings = new SettingsViewModel(clientConfig, () => IsSettingsOpen = false);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        ChooseNativeCommand = new RelayCommand(() => OnClientChosen(ClientMode.Native));
        ChooseWineCommand = new RelayCommand(() => OnClientChosen(ClientMode.Wine));
        ChangeClientCommand = new RelayCommand(OpenClientChoice);
        CancelClientChoiceCommand = new RelayCommand(CancelClientChoice);
    }

    public RelayCommand PlayCommand { get; }

    public RelayCommand RetryCommand { get; }

    public RelayCommand OpenSettingsCommand { get; }

    public RelayCommand ChooseNativeCommand { get; }

    public RelayCommand ChooseWineCommand { get; }

    public RelayCommand ChangeClientCommand { get; }

    public RelayCommand CancelClientChoiceCommand { get; }

    public SettingsViewModel Settings { get; }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        private set => SetField(ref _isSettingsOpen, value);
    }

    /// <summary>Whether the client-type (native vs Wine) chooser overlay is showing.</summary>
    public bool IsClientChoiceOpen
    {
        get => _isClientChoiceOpen;
        private set => SetField(ref _isClientChoiceOpen, value);
    }

    /// <summary>
    /// Cancel is offered only when re-opening the chooser to change an existing choice,
    /// not on the mandatory first-run prompt.
    /// </summary>
    public bool CanCancelClientChoice
    {
        get => _canCancelClientChoice;
        private set => SetField(ref _canCancelClientChoice, value);
    }

    /// <summary>The client chooser and its button only exist on Linux; Windows never sees them.</summary>
    public bool IsLinux => !_isWindows;

    // Title area driven by the BrandTitleMode branding resource (Text | Logo | None).
    // Constant per build, so plain getters (no change notification needed).
    public bool ShowTitleArea => !IsTitleMode("None");

    public bool ShowTextTitle => IsTitleMode("Text");

    public bool ShowLogo => IsTitleMode("Logo");

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

    /// <summary>
    /// Begins the flow when the window is shown: resolve which client to run (prompting
    /// once on Linux if the choice is not stored yet), then run the update. On Windows
    /// this resolves immediately to the Windows client - no prompt, no branching.
    /// </summary>
    public async Task StartAsync()
    {
        if (!_isWindows && _clientModeStore.Load() is null)
        {
            // First run on Linux: hold here until the player picks a client. The choice
            // provisions the updater/launcher and completes this task.
            _clientChoicePending = new TaskCompletionSource();
            OpenClientChoice(canCancel: false);
            await _clientChoicePending.Task;
        }
        else
        {
            var mode = _isWindows ? default : _clientModeStore.Load()!.Value;
            Provision(mode);
        }

        await RunUpdateAsync();
    }

    /// <summary>Cancels an in-flight update, e.g. when the window is closing.</summary>
    public void Cancel() => _cancellation.Cancel();

    // Builds the updater/launcher for the chosen client. On Windows the mode is ignored.
    private void Provision(ClientMode mode)
    {
        var profile = ClientProfileResolver.Resolve(_isWindows, mode);
        _updater = _updaterFactory(profile);
        _launcher = _launcherFactory(profile);
    }

    private void OpenClientChoice() => OpenClientChoice(canCancel: true);

    private void OpenClientChoice(bool canCancel)
    {
        CanCancelClientChoice = canCancel;
        IsClientChoiceOpen = true;
    }

    private void CancelClientChoice()
    {
        // Only reachable from the "change client" button (never the first-run prompt).
        IsClientChoiceOpen = false;
    }

    private void OnClientChosen(ClientMode mode)
    {
        _clientModeStore.Save(mode);
        Provision(mode);
        IsClientChoiceOpen = false;

        if (_clientChoicePending is { } pending)
        {
            // First-run prompt: let StartAsync continue into the update.
            _clientChoicePending = null;
            pending.SetResult();
        }
        else
        {
            // Changed the client after the fact: re-check against the new manifest.
            _ = RunUpdateAsync();
        }
    }

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

            // Non-null: StartAsync provisions before the first update, and Retry only
            // fires after that.
            var result = await _updater!.UpdateAsync(progress, _cancellation.Token);
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
            // Non-null: Play is only enabled (CanPlay) after a successful update, which
            // runs after provisioning.
            _launcher!.Launch();
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

    private static bool IsTitleMode(string mode) =>
        string.Equals(Text("BrandTitleMode", "Text"), mode, StringComparison.OrdinalIgnoreCase);
}
