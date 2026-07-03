using Launcher.Core;
using Xunit;

namespace Launcher.Core.Tests;

public class ClientLaunchCommandTests
{
    [Fact]
    public void Create_OnWindows_RunsExecutableDirectly()
    {
        var command = ClientLaunchCommand.Create("/games/mu", "Main.exe", isWindows: true);

        Assert.Equal(Path.Combine("/games/mu", "Main.exe"), command.FileName);
        Assert.Empty(command.Arguments);
        Assert.Equal("/games/mu", command.WorkingDirectory);
        Assert.Empty(command.EnvironmentOverrides);
    }

    [Fact]
    public void Create_OnNonWindows_RunsBareNameThroughWine()
    {
        var command = ClientLaunchCommand.Create("/games/mu", "Main.exe", isWindows: false);

        // Bare name, resolved against the working directory - no absolute path.
        Assert.Equal("wine", command.FileName);
        Assert.Equal(["Main.exe"], command.Arguments);
        Assert.Equal("/games/mu", command.WorkingDirectory);
        Assert.Empty(command.EnvironmentOverrides);
    }

    [Fact]
    public void Create_OnNonWindows_AppliesWinePrefixAndCommandFromConfig()
    {
        var config = new LinuxLaunchConfig { WineCommand = "wine64", WinePrefix = "/home/user/.winetestowe" };

        var command = ClientLaunchCommand.Create("/games/mu", "Main.exe", isWindows: false, config);

        Assert.Equal("wine64", command.FileName);
        Assert.Equal(["Main.exe"], command.Arguments);
        Assert.Equal("/home/user/.winetestowe", command.EnvironmentOverrides["WINEPREFIX"]);
    }

    [Fact]
    public void Create_OnLinuxNative_RunsElfDirectlyWithoutWine()
    {
        var config = new LinuxLaunchConfig { Mode = ClientMode.Native };

        var command = ClientLaunchCommand.Create("/games/mu", "Main", isWindows: false, config);

        // Direct exec of the ELF, no wine wrapper, no arguments, no environment.
        Assert.Equal(Path.Combine("/games/mu", "Main"), command.FileName);
        Assert.Empty(command.Arguments);
        Assert.Equal("/games/mu", command.WorkingDirectory);
        Assert.Empty(command.EnvironmentOverrides);
    }

    [Fact]
    public void Create_OnLinuxWineMode_StillRunsThroughWine()
    {
        var config = new LinuxLaunchConfig { Mode = ClientMode.Wine, WinePrefix = "/home/user/.winep" };

        var command = ClientLaunchCommand.Create("/games/mu", "Main.exe", isWindows: false, config);

        Assert.Equal("wine", command.FileName);
        Assert.Equal(["Main.exe"], command.Arguments);
        Assert.Equal("/home/user/.winep", command.EnvironmentOverrides["WINEPREFIX"]);
    }

    // Invariant #0: on Windows the client always runs directly - the Mode field is
    // never consulted, so a native/wine value can never divert the Windows path.
    [Theory]
    [InlineData(ClientMode.Native)]
    [InlineData(ClientMode.Wine)]
    public void Create_OnWindows_IgnoresLinuxMode(ClientMode mode)
    {
        var config = new LinuxLaunchConfig { Mode = mode };

        var command = ClientLaunchCommand.Create("/games/mu", "Main.exe", isWindows: true, config);

        Assert.Equal(Path.Combine("/games/mu", "Main.exe"), command.FileName);
        Assert.Empty(command.Arguments);
        Assert.Empty(command.EnvironmentOverrides);
    }
}

public class ClientProfileResolverTests
{
    [Theory]
    [InlineData(ClientMode.Native)]
    [InlineData(ClientMode.Wine)]
    public void Resolve_OnWindows_AlwaysWindowsClient_RegardlessOfMode(ClientMode mode)
    {
        var profile = ClientProfileResolver.Resolve(isWindows: true, mode);

        Assert.Equal(LauncherConfig.ManifestUrl, profile.ManifestUrl);
        Assert.Equal(LauncherConfig.WindowsClientExecutableName, profile.ExecutableName);
    }

    [Fact]
    public void Resolve_OnLinuxNative_UsesLinuxManifestAndBareMain()
    {
        var profile = ClientProfileResolver.Resolve(isWindows: false, ClientMode.Native);

        Assert.Equal(LauncherConfig.LinuxManifestUrl, profile.ManifestUrl);
        Assert.Equal(LauncherConfig.NativeLinuxClientExecutableName, profile.ExecutableName);
    }

    [Fact]
    public void Resolve_OnLinuxWine_RunsWindowsClientFromWindowsManifest()
    {
        var profile = ClientProfileResolver.Resolve(isWindows: false, ClientMode.Wine);

        // Wine runs the Windows client, so it shares the Windows manifest + executable.
        Assert.Equal(LauncherConfig.ManifestUrl, profile.ManifestUrl);
        Assert.Equal(LauncherConfig.WindowsClientExecutableName, profile.ExecutableName);
    }
}

public class LinuxLaunchConfigTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("linuxcfg-test-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Load_WhenMissing_ReturnsDefaults()
    {
        var config = LinuxLaunchConfig.Load(_dir);

        Assert.Equal("wine", config.WineCommand);
        Assert.Null(config.WinePrefix);
    }

    [Fact]
    public void Load_ReadsPrefixAndCommand()
    {
        File.WriteAllText(
            Path.Combine(_dir, LinuxLaunchConfig.FileName),
            """{ "winePrefix": "/home/user/.winetestowe", "wineCommand": "wine64" }""");

        var config = LinuxLaunchConfig.Load(_dir);

        Assert.Equal("wine64", config.WineCommand);
        Assert.Equal("/home/user/.winetestowe", config.WinePrefix);
    }

    [Fact]
    public void Load_OnlyPrefix_KeepsDefaultCommand()
    {
        File.WriteAllText(
            Path.Combine(_dir, LinuxLaunchConfig.FileName),
            """{ "winePrefix": "/home/user/.winecustom" }""");

        var config = LinuxLaunchConfig.Load(_dir);

        Assert.Equal("wine", config.WineCommand);
        Assert.Equal("/home/user/.winecustom", config.WinePrefix);
    }

    [Fact]
    public void Load_WhenCorrupt_ReturnsDefaults()
    {
        File.WriteAllText(Path.Combine(_dir, LinuxLaunchConfig.FileName), "{ not valid json");

        var config = LinuxLaunchConfig.Load(_dir);

        Assert.Equal("wine", config.WineCommand);
        Assert.Null(config.WinePrefix);
    }

    [Fact]
    public void Mode_IsUnsetWhenAbsent()
    {
        Assert.Null(LinuxLaunchConfig.Load(_dir).Mode);
    }

    [Fact]
    public void Load_ReadsModeAsString()
    {
        File.WriteAllText(Path.Combine(_dir, LinuxLaunchConfig.FileName), """{ "mode": "native" }""");

        Assert.Equal(ClientMode.Native, LinuxLaunchConfig.Load(_dir).Mode);
    }

    [Fact]
    public void Save_RoundTripsModeAndKeepsWinePrefix()
    {
        // A player who had only a wine prefix, then picks a client type.
        new LinuxLaunchConfig { WinePrefix = "/home/user/.winep" }.Save(_dir);
        var stored = LinuxLaunchConfig.Load(_dir) with { Mode = ClientMode.Wine };
        stored.Save(_dir);

        var reloaded = LinuxLaunchConfig.Load(_dir);
        Assert.Equal(ClientMode.Wine, reloaded.Mode);
        Assert.Equal("/home/user/.winep", reloaded.WinePrefix);
    }

    [Fact]
    public void Save_OmitsModeWhenUnset()
    {
        new LinuxLaunchConfig { WinePrefix = "/home/user/.winep" }.Save(_dir);

        var text = File.ReadAllText(Path.Combine(_dir, LinuxLaunchConfig.FileName));
        Assert.DoesNotContain("mode", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LinuxClientModeStore_SavesAndLoadsChoice()
    {
        var store = new LinuxClientModeStore(_dir);
        Assert.Null(store.Load());

        store.Save(ClientMode.Native);

        Assert.Equal(ClientMode.Native, store.Load());
    }
}
