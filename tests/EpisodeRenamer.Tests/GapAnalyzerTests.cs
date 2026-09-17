using EpisodeRenamer.Core;

namespace EpisodeRenamer.Tests;

public class GapAnalyzerTests
{
    [Fact]
    public void EmptyList_ReturnsZeroCount()
    {
        var result = GapAnalyzer.Analyze(Array.Empty<int>());
        Assert.Equal(0, result.Count);
        Assert.Empty(result.Missing);
        Assert.Empty(result.Runs);
    }

    [Fact]
    public void ConsecutiveEpisodes_NoMissing()
    {
        var result = GapAnalyzer.Analyze(new[] { 1, 2, 3, 4, 5 });
        Assert.Equal(0, result.Count);
        Assert.Empty(result.Missing);
    }

    [Fact]
    public void GapsInSequence()
    {
        var result = GapAnalyzer.Analyze(new[] { 1, 2, 4, 5, 8 });
        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { 3, 6, 7 }, result.Missing);
        Assert.Equal(new[] { (3, 3), (6, 7) }, result.Runs);
    }

    [Fact]
    public void SingleMissing()
    {
        var result = GapAnalyzer.Analyze(new[] { 1, 3, 4 });
        Assert.Equal(1, result.Count);
        Assert.Equal(new[] { 2 }, result.Missing);
        Assert.Single(result.Runs);
        Assert.Equal((2, 2), result.Runs[0]);
    }

    [Fact]
    public void DuplicatesAreIgnored()
    {
        var result = GapAnalyzer.Analyze(new[] { 1, 1, 2, 3 });
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void MultipleRunsWithTail()
    {
        var result = GapAnalyzer.Analyze(new[] { 1, 5, 9 });
        Assert.Equal(6, result.Count);
        Assert.Equal(2, result.Runs.Count);
        Assert.Equal((2, 4), result.Runs[0]);
        Assert.Equal((6, 8), result.Runs[1]);
    }

    [Fact]
    public void PreCancelledToken_ReturnsImmediatelyForHugeRange()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = GapAnalyzer.Analyze(new[] { 1, 100000000 }, cts.Token);

        Assert.NotNull(result);
        Assert.True(result.Count >= 0);
    }
}
