using System.Text;
using Launcher.Core;

namespace Launcher.Core.Tests;

public class ClientConfigTests : IDisposable
{
    private const string Sample =
        "[LOGIN]\n" +
        "RememberMe=0\n" +
        "Language=Eng\n" +
        "EncryptedUsername=\n" +
        "EncryptedPassword=\n" +
        "[Window]\n" +
        "Width=1024\n" +
        "Height=768\n" +
        "Windowed=1\n" +
        "[Audio]\n" +
        "SoundVolume=5\n" +
        "MusicVolume=5\n" +
        "[CONNECTION SETTINGS]\n" +
        "ServerIP=127.127.127.127\n" +
        "ServerPort=44406\n";

    private readonly string _dir = Directory.CreateTempSubdirectory("config-test-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Read_ReturnsDefaults_WhenFileMissing()
    {
        var settings = new ClientConfig(_dir).Read();

        Assert.Equal(ClientSettings.Default, settings);
    }

    [Fact]
    public void Read_ParsesExistingValues()
    {
        WriteConfig(Sample.Replace("Width=1024", "Width=1920").Replace("Height=768", "Height=1080")
            .Replace("Windowed=1", "Windowed=0").Replace("SoundVolume=5", "SoundVolume=3"));

        var settings = new ClientConfig(_dir).Read();

        Assert.Equal(new ClientSettings(1920, 1080, false, 3, 5), settings);
    }

    [Fact]
    public void Read_ClampsOutOfRangeVolume()
    {
        WriteConfig(Sample.Replace("SoundVolume=5", "SoundVolume=99").Replace("MusicVolume=5", "MusicVolume=-4"));

        var settings = new ClientConfig(_dir).Read();

        Assert.Equal(ClientSettings.MaxVolume, settings.SoundVolume);
        Assert.Equal(ClientSettings.MinVolume, settings.MusicVolume);
    }

    [Fact]
    public void Write_UpdatesManagedKeys_AndLeavesEverythingElseUntouched()
    {
        WriteConfig(Sample);

        new ClientConfig(_dir).Write(new ClientSettings(1920, 1080, false, 8, 2));

        var text = ReadConfig();
        Assert.Contains("Width=1920", text);
        Assert.Contains("Height=1080", text);
        Assert.Contains("Windowed=0", text);
        Assert.Contains("SoundVolume=8", text);
        Assert.Contains("MusicVolume=2", text);
        // Untouched: connection + login survive verbatim.
        Assert.Contains("ServerIP=127.127.127.127", text);
        Assert.Contains("ServerPort=44406", text);
        Assert.Contains("Language=Eng", text);
    }

    [Fact]
    public void Write_DoesNotDuplicateKeysOrSections()
    {
        WriteConfig(Sample);

        new ClientConfig(_dir).Write(new ClientSettings(800, 600, true, 5, 5));

        var text = ReadConfig();
        Assert.Equal(1, Occurrences(text, "[Window]"));
        Assert.Equal(1, Occurrences(text, "[Audio]"));
        Assert.Equal(1, Occurrences(text, "Width="));
        Assert.Equal(1, Occurrences(text, "SoundVolume="));
    }

    [Fact]
    public void Write_RoundTripsValues()
    {
        WriteConfig(Sample);
        var config = new ClientConfig(_dir);
        var expected = new ClientSettings(1680, 1050, false, 0, 10);

        config.Write(expected);

        Assert.Equal(expected, config.Read());
    }

    [Fact]
    public void Write_AddsMissingSectionsAndKeys()
    {
        WriteConfig("[LOGIN]\nLanguage=Eng\n");

        new ClientConfig(_dir).Write(new ClientSettings(1280, 720, true, 7, 4));
        var settings = new ClientConfig(_dir).Read();

        Assert.Equal(new ClientSettings(1280, 720, true, 7, 4), settings);
        Assert.Contains("Language=Eng", ReadConfig()); // existing content kept
    }

    [Fact]
    public void Write_PreservesNonAsciiBytes_AndAddsNoBom()
    {
        // A non-ASCII byte (0xE9 = 'é' in Latin1) in an untouched field must survive.
        var bytes = Encoding.Latin1.GetBytes(Sample.Replace("EncryptedUsername=", "EncryptedUsername=café"));
        File.WriteAllBytes(ConfigPath, bytes);

        new ClientConfig(_dir).Write(new ClientSettings(1024, 768, true, 6, 6));

        var written = File.ReadAllBytes(ConfigPath);
        Assert.NotEqual(0xEF, written[0]); // no UTF-8 BOM
        Assert.NotEqual(0xFF, written[0]); // no UTF-16 BOM
        Assert.Contains((byte)0xE9, written); // the original byte is still there, unchanged
    }

    private string ConfigPath => Path.Combine(_dir, ClientConfig.FileName);

    private void WriteConfig(string content) => File.WriteAllBytes(ConfigPath, Encoding.Latin1.GetBytes(content));

    private string ReadConfig() => Encoding.Latin1.GetString(File.ReadAllBytes(ConfigPath));

    private static int Occurrences(string haystack, string needle)
    {
        var count = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
