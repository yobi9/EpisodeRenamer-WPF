using System.IO;
using EpisodeRenamer.Core;
using Xunit;

namespace EpisodeRenamer.Tests;

public class FileDiscoveryTests
{
    private readonly string _dir = TestHelpers.NewTempDir();

    [Fact]
    public void Discover_SeparatesVideoFromNonVideo()
    {
        TestHelpers.NewTempFileName(_dir, "episode01.mkv");
        TestHelpers.NewTempFileName(_dir, "episode02.mp4");
        TestHelpers.NewTempFileName(_dir, "subs.srt");
        TestHelpers.NewTempFileName(_dir, "notes.txt");

        DiscoveryResult r = FileDiscovery.Discover(_dir, recurse: false);

        Assert.Equal(4, r.All.Count);
        Assert.Equal(2, r.Video.Count);
        Assert.Equal(2, r.Ignored.Count);
        Assert.Contains(r.Ignored, f => f.Name == "subs.srt");
        Assert.Contains(r.Ignored, f => f.Name == "notes.txt");
    }

    [Fact]
    public void Discover_ExtensionlessFile_IsIgnored()
    {
        TestHelpers.NewTempFileName(_dir, "MAKEFILE");

        DiscoveryResult r = FileDiscovery.Discover(_dir, recurse: false);

        Assert.Empty(r.Video);
        Assert.Single(r.Ignored);
    }

    [Fact]
    public void Discover_Recurse_FindsNestedVideos()
    {
        string sub = Path.Combine(_dir, "Season 01");
        Directory.CreateDirectory(sub);
        TestHelpers.NewTempFileName(sub, "nested.avi");

        DiscoveryResult topOnly = FileDiscovery.Discover(_dir, recurse: false);
        DiscoveryResult recursive = FileDiscovery.Discover(_dir, recurse: true);

        Assert.DoesNotContain(topOnly.Video, f => f.Name == "nested.avi");
        Assert.Contains(recursive.Video, f => f.Name == "nested.avi");
        Assert.NotEqual(topOnly.Video.Count, recursive.Video.Count);
    }

    [Theory]
    [InlineData("file.mp4", true)]
    [InlineData("file.MKV", true)]
    [InlineData("file.ts", true)]
    [InlineData("file.webm", true)]
    [InlineData("file.txt", false)]
    [InlineData("file", false)]
    public void IsVideoFile_MatchesWhitelist(string name, bool expected)
    {
        string path = TestHelpers.NewTempFileName(_dir, name);
        Assert.Equal(expected, FileDiscovery.IsVideoFile(new FileInfo(path)));
    }
}