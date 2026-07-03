namespace Launcher.Core;

/// <summary>
/// Which client a Linux player runs. The choice is made once (a first-run prompt)
/// and stored in <c>launcher.local.json</c>; a null/unset value means "not chosen
/// yet, ask". It is meaningless on Windows, where the client is always the native
/// Windows build.
/// </summary>
public enum ClientMode
{
    /// <summary>The native Linux client (<c>Main</c> + <c>.so</c>), run directly.</summary>
    Native,

    /// <summary>The Windows client (<c>Main.exe</c>), run through Wine.</summary>
    Wine,
}
