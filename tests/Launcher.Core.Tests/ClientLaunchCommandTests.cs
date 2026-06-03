using Launcher.Core;
using Xunit;

namespace Launcher.Core.Tests;

public class ClientLaunchCommandTests
{
    [Fact]
    public void Create_OnWindows_RunsExecutableDirectly()
    {
        var command = ClientLaunchCommand.Create("/games/mu", "main.exe", isWindows: true);

        Assert.Equal(Path.Combine("/games/mu", "main.exe"), command.FileName);
        Assert.Empty(command.Arguments);
        Assert.Equal("/games/mu", command.WorkingDirectory);
    }

    [Fact]
    public void Create_OnNonWindows_RunsThroughWine()
    {
        var command = ClientLaunchCommand.Create("/games/mu", "main.exe", isWindows: false);

        Assert.Equal(ClientLaunchCommand.WineExecutable, command.FileName);
        Assert.Equal([Path.Combine("/games/mu", "main.exe")], command.Arguments);
        Assert.Equal("/games/mu", command.WorkingDirectory);
    }
}
