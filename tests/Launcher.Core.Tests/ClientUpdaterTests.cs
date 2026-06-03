using System.Text;
using Launcher.Core;
using Xunit;

namespace Launcher.Core.Tests;

public class ClientUpdaterTests : IDisposable
{
    private readonly string _clientDir = Directory.CreateTempSubdirectory("launcher-test-").FullName;

    public void Dispose() => Directory.Delete(_clientDir, recursive: true);

    [Fact]
    public async Task UpdateAsync_DownloadsMissingFiles()
    {
        var source = new FakePatchSource(new()
        {
            ["main.exe"] = Bytes("the client"),
            ["Data/Player/player.bmd"] = Bytes("player model"),
        });

        var result = await new ClientUpdater(source, _clientDir).UpdateAsync();

        Assert.Equal(2, result.UpdatedFileCount);
        Assert.False(result.WasUpToDate);
        Assert.Equal("the client", File.ReadAllText(Path.Combine(_clientDir, "main.exe")));
        Assert.Equal("player model", File.ReadAllText(Path.Combine(_clientDir, "Data", "Player", "player.bmd")));
    }

    [Fact]
    public async Task UpdateAsync_DownloadsNothingWhenAlreadyUpToDate()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["main.exe"] = Bytes("the client"),
            ["Data/info.txt"] = Bytes("hello"),
        };
        WriteLocal(files);
        var source = new FakePatchSource(files);

        var result = await new ClientUpdater(source, _clientDir).UpdateAsync();

        Assert.True(result.WasUpToDate);
        Assert.Equal(0, source.OpenCount);
    }

    [Fact]
    public async Task UpdateAsync_RedownloadsChangedFileOnly()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["main.exe"] = Bytes("v2 client"),
            ["Data/info.txt"] = Bytes("unchanged"),
        };
        // Local main.exe is stale; Data/info.txt already matches.
        WriteLocal(new() { ["main.exe"] = Bytes("v1 client"), ["Data/info.txt"] = Bytes("unchanged") });
        var source = new FakePatchSource(files);

        var result = await new ClientUpdater(source, _clientDir).UpdateAsync();

        Assert.Equal(1, result.UpdatedFileCount);
        Assert.Equal(1, source.OpenCount);
        Assert.Equal("v2 client", File.ReadAllText(Path.Combine(_clientDir, "main.exe")));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsOnHashMismatch()
    {
        var source = new FakePatchSource(new() { ["main.exe"] = Bytes("the client") });
        source.CorruptPaths.Add("main.exe");

        var updater = new ClientUpdater(source, _clientDir);

        await Assert.ThrowsAsync<UpdateException>(() => updater.UpdateAsync());
        Assert.False(File.Exists(Path.Combine(_clientDir, "main.exe")));
        Assert.False(File.Exists(Path.Combine(_clientDir, "main.exe.download")));
    }

    [Fact]
    public async Task UpdateAsync_ReportsCompletedPhaseLast()
    {
        var source = new FakePatchSource(new() { ["main.exe"] = Bytes("the client") });
        var reports = new List<UpdateProgress>();
        var progress = new Progress<UpdateProgress>(reports.Add);

        await new ClientUpdater(source, _clientDir).UpdateAsync(progress);

        // Progress<T> marshals asynchronously; give queued callbacks a moment to drain.
        await Task.Delay(50);
        Assert.Contains(reports, r => r.Phase == UpdatePhase.FetchingManifest);
        Assert.Equal(UpdatePhase.Completed, reports[^1].Phase);
    }

    private void WriteLocal(Dictionary<string, byte[]> files)
    {
        foreach (var (path, bytes) in files)
        {
            var full = Path.Combine(_clientDir, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllBytes(full, bytes);
        }
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);
}
