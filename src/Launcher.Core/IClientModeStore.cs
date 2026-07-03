namespace Launcher.Core;

/// <summary>
/// Persists the player's client-type choice (native Linux vs Windows-via-Wine).
/// Abstracted so the view model can be exercised without touching the filesystem.
/// </summary>
public interface IClientModeStore
{
    /// <summary>The stored choice, or null when it has not been made yet (prompt).</summary>
    ClientMode? Load();

    /// <summary>Persists the choice, keeping any other local launch settings intact.</summary>
    void Save(ClientMode mode);
}

/// <summary>
/// File-backed <see cref="IClientModeStore"/> that reads/writes the client-type
/// choice inside <c>launcher.local.json</c> via <see cref="LinuxLaunchConfig"/>.
/// Only ever used on Linux.
/// </summary>
public sealed class LinuxClientModeStore : IClientModeStore
{
    private readonly string _clientDirectory;

    public LinuxClientModeStore(string clientDirectory) => _clientDirectory = clientDirectory;

    public ClientMode? Load() => LinuxLaunchConfig.Load(_clientDirectory).Mode;

    public void Save(ClientMode mode) =>
        (LinuxLaunchConfig.Load(_clientDirectory) with { Mode = mode }).Save(_clientDirectory);
}
