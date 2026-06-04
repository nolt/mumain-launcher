using System.Globalization;
using System.IO;

namespace Launcher.Core;

/// <summary>
/// Reads and writes the window/audio settings the launcher manages in the client's
/// config.ini, editing the file in place so everything else - server IP/port, saved
/// login, language - is preserved byte for byte. The client reads config.ini with
/// the Windows profile API, which keeps the file in its existing encoding with no
/// BOM; <see cref="IniDocument"/> mirrors that.
/// </summary>
public sealed class ClientConfig
{
    public const string FileName = "config.ini";

    private const string WindowSection = "Window";
    private const string AudioSection = "Audio";

    private readonly string _path;

    public ClientConfig(string clientDirectory)
    {
        _path = Path.Combine(clientDirectory, FileName);
    }

    /// <summary>Reads current settings, falling back to client defaults for anything missing.</summary>
    public ClientSettings Read()
    {
        if (!File.Exists(_path))
        {
            return ClientSettings.Default;
        }

        var document = IniDocument.Load(_path);
        var defaults = ClientSettings.Default;
        return new ClientSettings(
            document.GetInt(WindowSection, "Width", defaults.Width),
            document.GetInt(WindowSection, "Height", defaults.Height),
            document.GetBool(WindowSection, "Windowed", defaults.Windowed),
            ClampVolume(document.GetInt(AudioSection, "SoundVolume", defaults.SoundVolume)),
            ClampVolume(document.GetInt(AudioSection, "MusicVolume", defaults.MusicVolume)));
    }

    /// <summary>Writes the five managed keys, leaving the rest of the file untouched.</summary>
    public void Write(ClientSettings settings)
    {
        var document = File.Exists(_path) ? IniDocument.Load(_path) : IniDocument.Empty();
        document.Set(WindowSection, "Width", Int(settings.Width));
        document.Set(WindowSection, "Height", Int(settings.Height));
        document.Set(WindowSection, "Windowed", settings.Windowed ? "1" : "0");
        document.Set(AudioSection, "SoundVolume", Int(ClampVolume(settings.SoundVolume)));
        document.Set(AudioSection, "MusicVolume", Int(ClampVolume(settings.MusicVolume)));
        document.Save(_path);
    }

    private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static int ClampVolume(int level) => Math.Clamp(level, ClientSettings.MinVolume, ClientSettings.MaxVolume);
}
