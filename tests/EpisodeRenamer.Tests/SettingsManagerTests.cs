using System.IO;
using System.Text;
using EpisodeRenamer.Core;
using Xunit;

namespace EpisodeRenamer.Tests;

public class SettingsManagerTests
{
    private readonly string _dir = TestHelpers.NewTempDir();

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        string file = Path.Combine(_dir, "settings.json");
        var manager = new SettingsManager(file);
        manager.Save(new AppSettings
        {
            Path = @"C:\Shows\My Show",
            Show = "My Show",
            Style = 3,
            Recurse = true,
            CleanTags = true
        });

        AppSettings? loaded = manager.Load();

        Assert.NotNull(loaded);
        Assert.Equal(@"C:\Shows\My Show", loaded!.Path);
        Assert.Equal("My Show", loaded.Show);
        Assert.Equal(3, loaded.Style);
        Assert.True(loaded.Recurse);
        Assert.True(loaded.CleanTags);
    }

    [Fact]
    public void Load_MissingFile_ReturnsNull()
    {
        var manager = new SettingsManager(Path.Combine(_dir, "nope.json"));
        Assert.Null(manager.Load());
    }

    [Fact]
    public void Load_CorruptJson_ReturnsNull()
    {
        string file = Path.Combine(_dir, "bad.json");
        File.WriteAllText(file, "{ not valid json !!!");
        var manager = new SettingsManager(file);
        Assert.Null(manager.Load());
    }

    [Fact]
    public void Save_WritesUtf8Bom()
    {
        string file = Path.Combine(_dir, "bom.json");
        var manager = new SettingsManager(file);
        manager.Save(new AppSettings());

        byte[] bytes = File.ReadAllBytes(file);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        Assert.True(bytes.Length > 3);
    }
}