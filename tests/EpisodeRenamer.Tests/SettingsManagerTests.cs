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
            CleanTags = true,
            CustomPattern = "{show} S{season}E{ep3}",
            IgnorePatterns = "sample,trailer",
            RenameSubtitles = true
        });

        AppSettings? loaded = manager.Load();

        Assert.NotNull(loaded);
        Assert.Equal(@"C:\Shows\My Show", loaded!.Path);
        Assert.Equal("My Show", loaded.Show);
        Assert.Equal(3, loaded.Style);
        Assert.True(loaded.Recurse);
        Assert.True(loaded.CleanTags);
        Assert.Equal("{show} S{season}E{ep3}", loaded.CustomPattern);
        Assert.Equal("sample,trailer", loaded.IgnorePatterns);
        Assert.True(loaded.RenameSubtitles);
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

    [Fact]
    public void ExportThenLoad_RoundTrips()
    {
        string origFile = Path.Combine(_dir, "orig.json");
        string exportFile = Path.Combine(_dir, "exported.json");
        var manager = new SettingsManager(origFile);
        var settings = new AppSettings
        {
            Path = @"C:\Shows\X",
            Show = "X",
            Style = 5,
            Recurse = false,
            CleanTags = false,
            CustomPattern = "EP{ep3}",
            IgnorePatterns = "test",
            RenameSubtitles = false
        };
        manager.Save(settings);

        manager.SaveTo(exportFile, settings);
        AppSettings? loaded = manager.LoadFrom(exportFile);

        Assert.NotNull(loaded);
        Assert.Equal("X", loaded!.Show);
        Assert.Equal(5, loaded.Style);
        Assert.Equal("EP{ep3}", loaded.CustomPattern);
    }

    [Fact]
    public void LoadFrom_MissingFile_ReturnsNull()
    {
        var manager = new SettingsManager(Path.Combine(_dir, "x.json"));
        Assert.Null(manager.LoadFrom(Path.Combine(_dir, "nonexistent.json")));
    }
}