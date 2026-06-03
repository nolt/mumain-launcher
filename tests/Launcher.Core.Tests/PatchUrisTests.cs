using Launcher.Core;
using Xunit;

namespace Launcher.Core.Tests;

public class PatchUrisTests
{
    [Fact]
    public void ResolveFileUri_CombinesManifestDirectoryBaseUrlAndPath()
    {
        var manifest = new Uri("https://patch.example.com/version.json");

        var uri = PatchUris.ResolveFileUri(manifest, "files/", "Data/Player/player.bmd");

        Assert.Equal("https://patch.example.com/files/Data/Player/player.bmd", uri.AbsoluteUri);
    }

    [Fact]
    public void ResolveFileUri_AddsTrailingSlashToBaseUrl()
    {
        var manifest = new Uri("https://patch.example.com/version.json");

        var uri = PatchUris.ResolveFileUri(manifest, "files", "main.exe");

        Assert.Equal("https://patch.example.com/files/main.exe", uri.AbsoluteUri);
    }

    [Fact]
    public void ResolveFileUri_EscapesSpacesAndSpecialCharacters()
    {
        var manifest = new Uri("https://patch.example.com/version.json");

        var uri = PatchUris.ResolveFileUri(manifest, "files/", "Data/Effect/!Sword Eff.OZJ");

        Assert.Equal("https://patch.example.com/files/Data/Effect/%21Sword%20Eff.OZJ", uri.AbsoluteUri);
    }

    [Fact]
    public void ResolveFileUri_HonoursSubdirectoryManifestLocation()
    {
        var manifest = new Uri("https://patch.example.com/season6/version.json");

        var uri = PatchUris.ResolveFileUri(manifest, "files/", "main.exe");

        Assert.Equal("https://patch.example.com/season6/files/main.exe", uri.AbsoluteUri);
    }

    [Fact]
    public void ResolveSiblingUri_ResolvesNextToManifest()
    {
        var manifest = new Uri("https://patch.example.com/launcher.json");

        var uri = PatchUris.ResolveSiblingUri(manifest, "Launcher.App.exe");

        Assert.Equal("https://patch.example.com/Launcher.App.exe", uri.AbsoluteUri);
    }
}
