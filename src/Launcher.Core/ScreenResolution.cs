namespace Launcher.Core;

/// <summary>A selectable client window resolution.</summary>
public readonly record struct ScreenResolution(int Width, int Height)
{
    public override string ToString() => $"{Width} x {Height}";
}

/// <summary>
/// The resolutions the launcher offers. Mirrors the client's own list
/// (MuMain s_Resolutions) so the launcher exposes exactly what the client supports.
/// </summary>
public static class ScreenResolutions
{
    public static readonly IReadOnlyList<ScreenResolution> All =
    [
        new(640, 480),
        new(800, 600),
        new(1024, 768),
        new(1280, 720),
        new(1280, 1024),
        new(1600, 900),
        new(1600, 1200),
        new(1680, 1050),
        new(1920, 1080),
        new(2560, 1440),
    ];
}
