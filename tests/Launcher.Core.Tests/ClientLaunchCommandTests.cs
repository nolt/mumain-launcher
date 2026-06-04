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
}
