using Launcher.Core;

namespace Launcher.App.ViewModels;

/// <summary>
/// Backs the in-window settings panel: resolution, windowed mode and the two
/// volume sliders. Reads from and writes to the client's config.ini through
/// <see cref="ClientConfig"/>; touches nothing else in that file.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ClientConfig _config;
    private readonly Action _close;

    private ScreenResolution _resolution = new(ClientSettings.Default.Width, ClientSettings.Default.Height);
    private bool _windowed = ClientSettings.Default.Windowed;
    private int _soundVolume = ClientSettings.Default.SoundVolume;
    private int _musicVolume = ClientSettings.Default.MusicVolume;

    public SettingsViewModel(ClientConfig config, Action close)
    {
        _config = config;
        _close = close;
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(close);
    }

    public IReadOnlyList<ScreenResolution> Resolutions => ScreenResolutions.All;

    public int MinVolume => ClientSettings.MinVolume;

    public int MaxVolume => ClientSettings.MaxVolume;

    public RelayCommand SaveCommand { get; }

    public RelayCommand CancelCommand { get; }

    public ScreenResolution Resolution
    {
        get => _resolution;
        set => SetField(ref _resolution, value);
    }

    public bool Windowed
    {
        get => _windowed;
        set => SetField(ref _windowed, value);
    }

    public int SoundVolume
    {
        get => _soundVolume;
        set => SetField(ref _soundVolume, value);
    }

    public int MusicVolume
    {
        get => _musicVolume;
        set => SetField(ref _musicVolume, value);
    }

    /// <summary>Loads the current values from config.ini. Called each time the panel opens.</summary>
    public void Load()
    {
        var current = _config.Read();
        Resolution = ScreenResolutions.All.FirstOrDefault(
            r => r.Width == current.Width && r.Height == current.Height,
            new ScreenResolution(current.Width, current.Height));
        Windowed = current.Windowed;
        SoundVolume = current.SoundVolume;
        MusicVolume = current.MusicVolume;
    }

    private void Save()
    {
        _config.Write(new ClientSettings(Resolution.Width, Resolution.Height, Windowed, SoundVolume, MusicVolume));
        _close();
    }
}
