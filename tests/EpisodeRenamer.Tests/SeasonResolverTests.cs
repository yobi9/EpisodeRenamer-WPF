using EpisodeRenamer.Core;

namespace EpisodeRenamer.Tests;

public class SeasonResolverTests
{
    [Theory]
    [InlineData("Season 1", 1)]
    [InlineData("Season 05", 5)]
    [InlineData("Season 12", 12)]
    public void SeasonWithDigits(string name, int expected)
        => Assert.Equal(expected, SeasonResolver.Resolve(name));

    [Theory]
    [InlineData("Season III", 3)]
    [InlineData("Season IV", 4)]
    [InlineData("Season IX", 9)]
    public void SeasonWithRomanNumerals(string name, int expected)
        => Assert.Equal(expected, SeasonResolver.Resolve(name));

    [Theory]
    [InlineData("Season Three", 3)]
    [InlineData("Season Twelve", 12)]
    [InlineData("Season One", 1)]
    public void SeasonWithEnglishWords(string name, int expected)
        => Assert.Equal(expected, SeasonResolver.Resolve(name));

    [Theory]
    [InlineData("\u0627\u0644\u0645\u0648\u0633\u0645 \u0661", 1)]
    [InlineData("\u0627\u0644\u0645\u0648\u0633\u0645 \u0663", 3)]
    [InlineData("\u0627\u0644\u0645\u0648\u0633\u0645 \u0669", 9)]
    public void SeasonWithArabicIndicDigits(string name, int expected)
        => Assert.Equal(expected, SeasonResolver.Resolve(name));

    [Theory]
    [InlineData("\u0627\u0644\u0645\u0648\u0633\u0645 2", 2)]
    [InlineData("\u0645\u0648\u0633\u0645 7", 7)]
    public void SeasonWithLatinDigits(string name, int expected)
        => Assert.Equal(expected, SeasonResolver.Resolve(name));

    [Theory]
    [InlineData("\u0627\u0644\u0645\u0648\u0633\u0645 \u0627\u0644\u0623\u0648\u0644", 1)]
    [InlineData("\u0627\u0644\u0645\u0648\u0633\u0645 \u0627\u0644\u062e\u0627\u0645\u0633", 5)]
    [InlineData("\u0627\u0644\u0645\u0648\u0633\u0645 \u0627\u0644\u0639\u0627\u0634\u0631", 10)]
    [InlineData("\u0627\u0644\u0645\u0648\u0633\u0645 \u0627\u0644\u062d\u0627\u062f\u064a \u0639\u0634\u0631", 11)]
    [InlineData("\u0627\u0644\u0645\u0648\u0633\u0645 \u0627\u0644\u0639\u0634\u0631\u0648\u0646", 20)]
    public void SeasonWithArabicWords(string name, int expected)
        => Assert.Equal(expected, SeasonResolver.Resolve(name));

    [Theory]
    [InlineData("\u0627\u0644\u062c\u0632\u0621 2", 2)]
    [InlineData("\u062c\u0632\u0621 5", 5)]
    public void PartDigits(string name, int expected)
        => Assert.Equal(expected, SeasonResolver.Resolve(name));

    [Theory]
    [InlineData("S02", 2)]
    [InlineData("S12", 12)]
    public void ShortcutS(string name, int expected)
        => Assert.Equal(expected, SeasonResolver.Resolve(name));

    [Fact]
    public void STandalone_WithSE_ReturnsSeason()
        => Assert.Equal(3, SeasonResolver.Resolve("SE3"));

    [Theory]
    [InlineData("Part 3", 3)]
    [InlineData("Pt 2", 2)]
    public void PartKeyword(string name, int expected)
        => Assert.Equal(expected, SeasonResolver.Resolve(name));

    [Theory]
    [InlineData("08", 8)]
    [InlineData("5", 5)]
    public void PureDigitFolder(string name, int expected)
        => Assert.Equal(expected, SeasonResolver.Resolve(name));

    [Fact]
    public void AllowGeneric_PicksFirstNumber()
        => Assert.Equal(7, SeasonResolver.Resolve("My Show 7", allowGeneric: true));

    [Fact]
    public void NoMatch_ReturnsNull()
        => Assert.Null(SeasonResolver.Resolve("My Show Name"));

    [Fact]
    public void Null_ReturnsNull()
        => Assert.Null(SeasonResolver.Resolve(null));
}
