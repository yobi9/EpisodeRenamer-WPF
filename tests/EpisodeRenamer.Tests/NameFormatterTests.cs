using EpisodeRenamer.Core;

namespace EpisodeRenamer.Tests;

public class NameFormatterTests
{
    [Fact]
    public void S01E01Style_PadsSeasonTo2AndEpisodeTo3()
        => Assert.Equal("S01E001", NameFormatter.Format(1, 1, "S01E01 (\u0646\u0645\u0637\u0628 \u0628\u0644\u064a\u0643\u0633)"));

    [Fact]
    public void S01E01Style_LargeSeasonAndEpisode()
        => Assert.Equal("S12E234", NameFormatter.Format(12, 234, "S01E01 (\u0646\u0645\u0637\u0628 \u0628\u0644\u064a\u0643\u0633)"));

    [Fact]
    public void S01DotE01Style()
        => Assert.Equal("S02.E003", NameFormatter.Format(2, 3, "S01.E01 (\u0646\u0645\u0637\u0628 \u0627\u0644\u062a\u0648\u0631\u0646\u062a)"));

    [Fact]
    public void SeasonEpisodeStyle_PadsEpisodeTo2()
        => Assert.Equal("Season 01 Episode 05", NameFormatter.Format(1, 5, "Season 01 Episode 01 (\u0648\u0635\u0641\u064a)"));

    [Fact]
    public void EP001Style_PadsTo3()
        => Assert.Equal("EP 007", NameFormatter.Format(1, 7, "EP 001 (\u0623\u0646\u0645\u064a)"));

    [Fact]
    public void Hash01Style_PadsTo2()
        => Assert.Equal("#03", NameFormatter.Format(1, 3, "#01 (\u0623\u0646\u0645\u064a \u0645\u062e\u062a\u0635\u0631)"));

    [Fact]
    public void ArabicStyle()
        => Assert.Equal("\u0627\u0644\u0645\u0648\u0633\u0645 01 \u0627\u0644\u062d\u0644\u0642\u0629 03",
            NameFormatter.Format(1, 3, "\u0627\u0644\u0645\u0648\u0633\u0645 01 - \u0627\u0644\u062d\u0644\u0642\u0629 01 (\u0639\u0631\u0628\u064a)"));

    [Fact]
    public void S1E1Style_UsesRawNumbers()
        => Assert.Equal("S1E5", NameFormatter.Format(1, 5, "S1E1 (\u0628\u062f\u0648\u0646 \u0623\u0635\u0641\u0627\u0631)"));

    [Fact]
    public void EP01Style_PadsTo2()
        => Assert.Equal("EP05", NameFormatter.Format(1, 5, "EP01 (\u0623\u0646\u0645\u064a)"));

    [Fact]
    public void DefaultStyle_FallsBackToS01E001()
        => Assert.Equal("S01E001", NameFormatter.Format(1, 1, null));

    [Fact]
    public void DefaultStyle_UnknownInputFallsBack()
        => Assert.Equal("S05E100", NameFormatter.Format(5, 100, "UnknownStyle"));

    [Theory]
    [InlineData("{show} S{season}E{ep3}", 3, 7, "My Show", "My Show S3E007")]
    [InlineData("E{ep2} - {show}", 1, 42, "Test", "E42 - Test")]
    [InlineData("{season}x{ep}", 2, 15, null, "2x15")]
    [InlineData("Custom {ep3}", 1, 9, "Ignored", "Custom 009")]
    public void FormatCustomTemplate_ReplacesTokens(string template, int season, int episode, string? show, string expected)
        => Assert.Equal(expected, NameFormatter.FormatCustomTemplate(season, episode, show, template));

    [Fact]
    public void FormatCustomTemplate_NullTemplate_FallsBackToDefault()
        => Assert.Equal("S01E003", NameFormatter.FormatCustomTemplate(1, 3, null, null));

    [Fact]
    public void FormatCustomTemplate_EmptyTemplate_FallsBackToDefault()
        => Assert.Equal("S01E003", NameFormatter.FormatCustomTemplate(1, 3, null, ""));

    [Fact]
    public void Format_WithSecondEpisode_UsesSameStyleAndHyphen()
        => Assert.Equal("S01E055-S01E056", NameFormatter.Format(1, 55, 56, "S01E01 (\u0646\u0645\u0637\u0628)"));

    [Fact]
    public void Format_WithSecondEpisode_ArabicStyle()
        => Assert.Equal(
            "\u0627\u0644\u0645\u0648\u0633\u0645 01 \u0627\u0644\u062d\u0644\u0642\u0629 55-\u0627\u0644\u0645\u0648\u0633\u0645 01 \u0627\u0644\u062d\u0644\u0642\u0629 56",
            NameFormatter.Format(1, 55, 56, "\u0627\u0644\u0645\u0648\u0633\u0645 01 - \u0627\u0644\u062d\u0644\u0642\u0629 01 (\u0639\u0631\u0628\u064a)"));

    [Theory]
    [InlineData("S01E{ep3}-E{epEnd3}", "S01E055-E056")]
    [InlineData("{ep2}/{epEnd2}", "55/56")]
    [InlineData("{ep}-{epEnd}", "55-56")]
    public void FormatCustomTemplate_EpEndTokens(string template, string expected)
        => Assert.Equal(expected, NameFormatter.FormatCustomTemplate(1, 55, 56, null, template));

    [Fact]
    public void FormatCustomTemplate_NoEndToken_DuplicatesTemplateForBothEpisodes()
        => Assert.Equal("E055-E056", NameFormatter.FormatCustomTemplate(1, 55, 56, null, "E{ep3}"));
}
