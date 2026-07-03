namespace Launcher.Core;

/// <summary>
/// The concrete client the launcher provisions and starts: which patch manifest
/// to update from and which executable to run. It is the single value that both
/// the updater and the launcher are built around, so the native-vs-wine decision
/// is made once and flows from here.
/// </summary>
/// <param name="ManifestUrl">Patch manifest to update against (version.json / version-linux.json).</param>
/// <param name="ExecutableName">Client executable to launch (Main.exe or Main).</param>
public sealed record ClientProfile(string ManifestUrl, string ExecutableName);

/// <summary>
/// Maps the platform and the player's client choice to a <see cref="ClientProfile"/>.
/// Pure and side-effect free so it is trivially testable.
/// </summary>
/// <remarks>
/// Invariant: on Windows the client is always the native Windows build - the
/// <paramref name="mode"/> argument is ignored and no native/wine branching happens.
/// The native-vs-wine split lives entirely in the non-Windows path. Wine runs the
/// Windows client, so it shares the Windows manifest and executable name; only the
/// native Linux client uses the Linux manifest and the bare <c>Main</c> binary.
/// </remarks>
public static class ClientProfileResolver
{
    public static ClientProfile Resolve(bool isWindows, ClientMode mode)
    {
        if (isWindows)
        {
            return new ClientProfile(LauncherConfig.ManifestUrl, LauncherConfig.WindowsClientExecutableName);
        }

        return mode == ClientMode.Native
            ? new ClientProfile(LauncherConfig.LinuxManifestUrl, LauncherConfig.NativeLinuxClientExecutableName)
            : new ClientProfile(LauncherConfig.ManifestUrl, LauncherConfig.WindowsClientExecutableName);
    }
}
