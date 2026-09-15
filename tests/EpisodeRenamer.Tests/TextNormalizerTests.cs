using EpisodeRenamer.Core;

namespace EpisodeRenamer.Tests;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("\u0660", "0")]
    [InlineData("\u0661", "1")]
    [InlineData("\u0662", "2")]
    [InlineData("\u0663", "3")]
    [InlineData("\u0664", "4")]
    [InlineData("\u0665", "5")]
    [InlineData("\u0666", "6")]
    [InlineData("\u0667", "7")]
    [InlineData("\u0668", "8")]
    [InlineData("\u0669", "9")]
    public void ConvertLatinDigits_ConvertsEachArabicIndicDigit(string arabic, string latin)
        => Assert.Equal(latin, TextNormalizer.ConvertLatinDigits(arabic));

    [Theory]
    [InlineData("\u0661\u0662\u0663", "123")]
    [InlineData("Show \u0661\u0662\u0667", "Show 127")]
    public void ConvertLatinDigits_ConvertsMultiDigit(string input, string expected)
        => Assert.Equal(expected, TextNormalizer.ConvertLatinDigits(input));

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void ConvertLatinDigits_NullOrEmptyReturnsInput(string? input, string? expected)
        => Assert.Equal(expected, TextNormalizer.ConvertLatinDigits(input!));

    [Theory]
    [InlineData("Show.Name", "Show Name")]
    [InlineData("Show_Name", "Show Name")]
    [InlineData("Show-Name", "Show Name")]
    [InlineData("Show–Name", "Show Name")]
    [InlineData("Show—Name", "Show Name")]
    [InlineData("Show(Name)", "Show Name")]
    [InlineData("Show[Name]", "Show Name")]
    public void Normalize_ConvertsSeparatorsToSpace(string input, string expected)
        => Assert.Equal(expected, TextNormalizer.Normalize(input));

    [Fact]
    public void Normalize_RemovesKashida()
        => Assert.Equal("\u0627\u0644\u0645\u0648\u0633\u0645", TextNormalizer.Normalize("\u0627\u0644\u0640\u0645\u0648\u0633\u0640\u0645"));

    [Theory]
    [InlineData("Show  Name", "Show Name")]
    [InlineData("Show   Name", "Show Name")]
    public void Normalize_CollapsesMultipleSpaces(string input, string expected)
        => Assert.Equal(expected, TextNormalizer.Normalize(input));

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", "  ")]
    public void Normalize_NullOrWhitespaceReturnsInput(string? input, string? expected)
        => Assert.Equal(expected, TextNormalizer.Normalize(input));

    [Fact]
    public void Normalize_ConvertsArabicDigitsAndStripsSeparators()
        => Assert.Equal("Show 123", TextNormalizer.Normalize("Show.\u0661\u0662\u0663"));

    [Theory]
    [InlineData("[1080p] Show", "Show")]
    [InlineData("Show [WEB-DL]", "Show")]
    [InlineData("Show (BluRay)", "Show ()")]
    public void RemoveSourceTags_RemovesTargetPatterns(string input, string expected)
        => Assert.Equal(expected, TextNormalizer.RemoveSourceTags(input));

    [Fact]
    public void RemoveSourceTags_RemovesParentheses()
        => Assert.Equal("Show", TextNormalizer.RemoveSourceTags("Show [HD]"));
}
