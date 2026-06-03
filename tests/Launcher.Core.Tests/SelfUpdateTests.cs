using System.Text;
using Launcher.Core;
using Launcher.Core.SelfUpdate;
using Xunit;

namespace Launcher.Core.Tests;

public class RuntimePlatformTests
{
    [Fact]
    public void RidFor_Windows_IsWinX64() => Assert.Equal("win-x64", RuntimePlatform.RidFor(isWindows: true));

    [Fact]
    public void RidFor_NonWindows_IsLinuxX64() => Assert.Equal("linux-x64", RuntimePlatform.RidFor(isWindows: false));
}

public class LauncherManifestSerializerTests
{
    [Fact]
    public void Deserialize_ReadsVersionAndPerRidFiles()
    {
        const string json = """
            {
              "version": "2026.06.10",
              "files": {
                "win-x64":   { "path": "Launcher.App.exe", "hash": "aa", "size": 10 },
                "linux-x64": { "path": "Launcher.App",      "hash": "bb", "size": 11 }
              }
            }
            """;

        var manifest = LauncherManifestSerializer.Deserialize(json);

        Assert.NotNull(manifest);
        Assert.Equal("2026.06.10", manifest!.Version);
        Assert.Equal("Launcher.App.exe", manifest.Files["win-x64"].Path);
        Assert.Equal("bb", manifest.Files["linux-x64"].Hash);
        Assert.Equal(11, manifest.Files["linux-x64"].Size);
    }

    [Fact]
    public void SerializeRoundTrip_Preserves()
    {
        var original = new LauncherManifest("2026.06.10", new()
        {
            ["win-x64"] = new LauncherFile("Launcher.App.exe", "aa", 10),
        });

        var roundTripped = LauncherManifestSerializer.Deserialize(LauncherManifestSerializer.Serialize(original));

        Assert.NotNull(roundTripped);
        Assert.Equal(original.Version, roundTripped!.Version);
        Assert.Equal(original.Files["win-x64"], roundTripped.Files["win-x64"]);
    }
}

public class LauncherSelfUpdaterDecisionTests
{
    private static LauncherManifest Manifest(string version) =>
        new(version, new() { ["win-x64"] = new LauncherFile("Launcher.App.exe", "aa", 10) });

    [Fact]
    public void NeedsUpdate_FalseWhenVersionsMatch() =>
        Assert.False(LauncherSelfUpdater.NeedsUpdate("2026.06.10", Manifest("2026.06.10")));

    [Fact]
    public void NeedsUpdate_TrueWhenVersionsDiffer() =>
        Assert.True(LauncherSelfUpdater.NeedsUpdate("2026.06.09", Manifest("2026.06.10")));
}

public class LauncherSwapTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("swap-test-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Apply_ReplacesCurrentWithNewAndKeepsOld()
    {
        var current = Path.Combine(_dir, "Launcher.App");
        File.WriteAllText(current, "old version");
        File.WriteAllText(LauncherSwap.NewPathFor(current), "new version");

        LauncherSwap.Apply(current);

        Assert.Equal("new version", File.ReadAllText(current));
        Assert.Equal("old version", File.ReadAllText(current + LauncherSwap.OldSuffix));
        Assert.False(File.Exists(LauncherSwap.NewPathFor(current)));
    }

    [Fact]
    public void Apply_OverwritesLeftoverOldFromPreviousSwap()
    {
        var current = Path.Combine(_dir, "Launcher.App");
        File.WriteAllText(current, "current");
        File.WriteAllText(current + LauncherSwap.OldSuffix, "stale leftover");
        File.WriteAllText(LauncherSwap.NewPathFor(current), "newest");

        LauncherSwap.Apply(current);

        Assert.Equal("newest", File.ReadAllText(current));
        Assert.Equal("current", File.ReadAllText(current + LauncherSwap.OldSuffix));
    }

    [Fact]
    public void CleanupOld_RemovesOldBinary()
    {
        var current = Path.Combine(_dir, "Launcher.App");
        File.WriteAllText(current + LauncherSwap.OldSuffix, Encoding.UTF8.GetString([1, 2, 3]));

        LauncherSwap.CleanupOld(current);

        Assert.False(File.Exists(current + LauncherSwap.OldSuffix));
    }

    [Fact]
    public void CleanupOld_NoOpWhenNothingToRemove()
    {
        var current = Path.Combine(_dir, "Launcher.App");

        LauncherSwap.CleanupOld(current);

        Assert.False(File.Exists(current + LauncherSwap.OldSuffix));
    }
}
