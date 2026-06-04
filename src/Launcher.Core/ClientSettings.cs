namespace Launcher.Core;

/// <summary>
/// The client settings the launcher can edit in config.ini. Volume is on the
/// client's 0..10 scale (0 = off). Everything else in config.ini - server IP/port,
/// saved login, language - is outside this record and left untouched.
/// </summary>
public sealed record ClientSettings(int Width, int Height, bool Windowed, int SoundVolume, int MusicVolume)
{
    public const int MinVolume = 0;
    public const int MaxVolume = 10;

    /// <summary>Matches the client's own defaults (GameConfigConstants).</summary>
    public static readonly ClientSettings Default = new(1024, 768, true, 5, 5);
}
