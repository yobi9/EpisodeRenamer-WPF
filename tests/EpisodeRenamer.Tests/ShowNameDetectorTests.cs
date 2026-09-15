using EpisodeRenamer.Core;

namespace EpisodeRenamer.Tests;

public class ShowNameDetectorTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void NullOrEmptyPath_ReturnsEmpty(string? path, string expected)
        => Assert.Equal(expected, ShowNameDetector.Detect(path));

    [Fact]
    public void DetectsShowName_RemovesSeasonSuffix()
    {
        string result = ShowNameDetector.Detect("C:\\Shows\\My Show Season 01");
        Assert.Equal("My Show", result);
    }

    [Fact]
    public void DetectsShowName_RemovesSxxExxSuffix()
    {
        string result = ShowNameDetector.Detect("C:\\Shows\\My.Show.S01E01");
        Assert.Equal("My Show", result);
    }

    [Fact]
    public void DetectsShowName_RemovesSxxSuffix()
    {
        string result = ShowNameDetector.Detect("C:\\Shows\\My Show S02");
        Assert.Equal("My Show", result);
    }

    [Fact]
    public void DetectsShowName_RemovesArabicSeasonSuffix()
    {
        string result = ShowNameDetector.Detect("C:\\Shows\\\u0645\u0633\u0644\u0633\u0644 \u0627\u0644\u0645\u0648\u0633\u0645 \u0627\u0644\u062e\u0627\u0645\u0633");
        Assert.Equal("\u0645\u0633\u0644\u0633\u0644", result);
    }

    [Fact]
    public void DetectsShowName_RemovesArabicNumberedSeason()
    {
        string result = ShowNameDetector.Detect("C:\\Shows\\\u0645\u0633\u0644\u0633\u0644 \u0627\u0644\u0645\u0648\u0633\u0645 \u0663");
        Assert.Equal("\u0645\u0633\u0644\u0633\u0644", result);
    }

    [Fact]
    public void DetectsShowName_RemovesBrackets()
    {
        string result = ShowNameDetector.Detect("C:\\Shows\\My Show [1080p]");
        Assert.Equal("My Show 1080p", result);
    }

    [Fact]
    public void DetectsShowName_SimpleLeaf()
    {
        string result = ShowNameDetector.Detect("C:\\Shows\\SimpleShow");
        Assert.Equal("SimpleShow", result);
    }

    [Fact]
    public void DetectsShowName_DotsToSpaces()
    {
        string result = ShowNameDetector.Detect("C:\\Shows\\My.Show.Name");
        Assert.Equal("My Show Name", result);
    }
}
