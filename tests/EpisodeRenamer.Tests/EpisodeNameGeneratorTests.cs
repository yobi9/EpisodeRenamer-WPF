using System.IO;
using EpisodeRenamer.Core;

namespace EpisodeRenamer.Tests;

public class EpisodeNameGeneratorTests
{
    private static string NewTempDir()
    {
        var rnd = new Random();
        const string alpha = "abcdefghijklmnopqrstuvwxyz";
        return Path.Combine(Path.GetTempPath(), "ER" + new string(Enumerable.Range(0, 12).Select(_ => alpha[rnd.Next(alpha.Length)]).ToArray()));
    }

    [Fact]
    public void GetSeasonNumber_FromFilename()
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, "Show.S02E05.mkv"));
            int? season = EpisodeNameGenerator.GetSeasonNumber(fi, dir.FullName);
            Assert.Equal(2, season);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void GetSeasonNumber_FromParentFolder()
    {
        var tmp = NewTempDir();
        var seasonDir = Directory.CreateDirectory(Path.Combine(tmp, "Season 03"));
        try
        {
            var fi = new FileInfo(Path.Combine(seasonDir.FullName, "Episode 01.mkv"));
            int? season = EpisodeNameGenerator.GetSeasonNumber(fi, tmp);
            Assert.Equal(3, season);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void GetSeasonNumber_DefaultsToNull_WhenNoMatch()
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, "movie.mkv"));
            int? season = EpisodeNameGenerator.GetSeasonNumber(fi, dir.FullName);
            Assert.Null(season);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void GenerateNewName_WithShowName()
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, "Show.S01E03.mkv"));
            var (ep, name) = EpisodeNameGenerator.GenerateNewName(fi, tmp, "S01E01 (\u0646\u0645\u0637\u0628)", "My Show", false);
            Assert.Equal(3, ep);
            Assert.Equal("My Show-S01E003.mkv", name);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void GenerateNewName_WithoutShowName()
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, "Show.S01E03.mkv"));
            var (ep, name) = EpisodeNameGenerator.GenerateNewName(fi, tmp, "S01E01 (\u0646\u0645\u0637\u0628 \u0628\u0644\u064a\u0643\u0633)", null, false);
            Assert.Equal(3, ep);
            Assert.Equal("S01E003.mkv", name);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void GenerateNewName_WithCleanTags()
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, "Show.S01E03.mkv"));
            var (ep, name) = EpisodeNameGenerator.GenerateNewName(fi, tmp, "S01E01 (\u0646\u0645\u0637\u0628)", "My Show [1080p] WEB-DL", true);
            Assert.Equal(3, ep);
            Assert.Equal("My Show-S01E003.mkv", name);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void GenerateNewName_NoEpisode_ReturnsNulls()
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, "movie.mkv"));
            var (ep, name) = EpisodeNameGenerator.GenerateNewName(fi, tmp, "S01E01 (\u0646\u0645\u0637\u0628)", "Show", false);
            Assert.Null(ep);
            Assert.Null(name);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void GenerateNewName_ArabicEpisode()
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, "\u0645\u0633\u0644\u0633\u0644 \u062d\u0644\u0642\u0629 5.mkv"));
            var (ep, name) = EpisodeNameGenerator.GenerateNewName(fi, tmp, "S01E01 (\u0646\u0645\u0637\u0628)", null, false);
            Assert.Equal(5, ep);
            Assert.Equal("S01E005.mkv", name);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void GenerateNewName_S1E1Style()
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, "Show.S01E03.mkv"));
            var (ep, name) = EpisodeNameGenerator.GenerateNewName(fi, tmp, "S1E1 (\u0628\u062f\u0648\u0646 \u0623\u0635\u0641\u0627\u0631)", null, false);
            Assert.Equal(3, ep);
            Assert.Equal("S1E3.mkv", name);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void GenerateNewNameInfo_DoubleEpisode_PlusSign()
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, "Show.S01E55+56.mkv"));
            var (ep, epEnd, name) = EpisodeNameGenerator.GenerateNewNameInfo(fi, tmp, "S01E01 (\u0646\u0645\u0637)", "My Show", false);
            Assert.Equal(55, ep);
            Assert.Equal(56, epEnd);
            Assert.Equal("My Show-S01E055-S01E056.mkv", name);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void GenerateNewNameInfo_DoubleEpisode_Hyphen()
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, "Show.S01E55-56.mkv"));
            var (ep, epEnd, name) = EpisodeNameGenerator.GenerateNewNameInfo(fi, tmp, "S01E01 (\u0646\u0645\u0637)", null, false);
            Assert.Equal(55, ep);
            Assert.Equal(56, epEnd);
            Assert.Equal("S01E055-S01E056.mkv", name);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void GenerateNewNameInfo_DoubleEpisode_AdjacentE()
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, "Show.S01E55E56.mkv"));
            var (ep, epEnd, name) = EpisodeNameGenerator.GenerateNewNameInfo(fi, tmp, "S01E01 (\u0646\u0645\u0637)", null, false);
            Assert.Equal(55, ep);
            Assert.Equal(56, epEnd);
            Assert.Equal("S01E055-S01E056.mkv", name);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Theory]
    [InlineData("Show.S01E05.1080p.mkv")]
    [InlineData("Show.S01E05.x265.mkv")]
    [InlineData("Show.S01E05.720p.WEB.mkv")]
    public void GenerateNewNameInfo_DoesNotTreatResolutionAsDouble(string fileName)
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, fileName));
            var (ep, epEnd, name) = EpisodeNameGenerator.GenerateNewNameInfo(fi, tmp, "S01E01 (\u0646\u0645\u0637)", null, false);
            Assert.Equal(5, ep);
            Assert.Null(epEnd);
            Assert.Equal("S01E005.mkv", name);
        }
        finally { Directory.Delete(tmp, true); }
    }

    [Fact]
    public void GetEpisodeNumber_MultiEpisode_ReturnsFirst()
    {
        var tmp = NewTempDir();
        var dir = Directory.CreateDirectory(tmp);
        try
        {
            var fi = new FileInfo(Path.Combine(dir.FullName, "Show.S01E55+56.mkv"));
            Assert.Equal(55, EpisodeNameGenerator.GetEpisodeNumber(fi, tmp));
        }
        finally { Directory.Delete(tmp, true); }
    }
}
