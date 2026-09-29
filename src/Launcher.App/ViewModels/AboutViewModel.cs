using System.Reflection;
using Launcher.Core;

namespace Launcher.App.ViewModels;

/// <summary>
/// Backs the About panel (launcher version) and its Licenses list: the launcher's
/// own LICENSE, then every third-party one, both embedded in the binary.
/// </summary>
public sealed class AboutViewModel : ViewModelBase
{
    private IReadOnlyList<LicenseParagraph> _paragraphs = [];
    private bool _isLicensesShown;

    public AboutViewModel(string version, Action close)
    {
        Version = version;
        CloseCommand = new RelayCommand(close);
        ShowLicensesCommand = new RelayCommand(ShowLicenses);
        HideLicensesCommand = new RelayCommand(() => IsLicensesShown = false);
    }

    public string Version { get; }

    public RelayCommand CloseCommand { get; }

    public RelayCommand ShowLicensesCommand { get; }

    public RelayCommand HideLicensesCommand { get; }

    public bool IsLicensesShown
    {
        get => _isLicensesShown;
        private set => SetField(ref _isLicensesShown, value);
    }

    public IReadOnlyList<LicenseParagraph> Paragraphs
    {
        get => _paragraphs;
        private set => SetField(ref _paragraphs, value);
    }

    /// <summary>Opens on the About panel, not on the list left open last time.</summary>
    public void Reset() => IsLicensesShown = false;

    private void ShowLicenses()
    {
        // Read from the embedded resources the first time the list opens.
        if (Paragraphs.Count == 0)
        {
            Paragraphs = LicenseText.Split(Resource("LICENSE"), Resource("THIRD-PARTY-LICENSES.md"));
        }

        IsLicensesShown = true;
    }

    private static string Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
