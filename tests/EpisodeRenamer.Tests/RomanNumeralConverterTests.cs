using EpisodeRenamer.Core;

namespace EpisodeRenamer.Tests;

public class RomanNumeralConverterTests
{
    [Theory]
    [InlineData("I", 1)]
    [InlineData("V", 5)]
    [InlineData("X", 10)]
    [InlineData("L", 50)]
    [InlineData("C", 100)]
    [InlineData("D", 500)]
    [InlineData("M", 1000)]
    public void SingleDigit(string input, int expected)
        => Assert.Equal(expected, RomanNumeralConverter.Convert(input));

    [Theory]
    [InlineData("IV", 4)]
    [InlineData("IX", 9)]
    [InlineData("XIV", 14)]
    [InlineData("XL", 40)]
    [InlineData("XC", 90)]
    [InlineData("CD", 400)]
    [InlineData("CM", 900)]
    public void SubtractiveNotation(string input, int expected)
        => Assert.Equal(expected, RomanNumeralConverter.Convert(input));

    [Fact]
    public void MCMXCIX_Returns1999()
        => Assert.Equal(1999, RomanNumeralConverter.Convert("MCMXCIX"));

    [Theory]
    [InlineData("iii", 3)]
    [InlineData("xiv", 14)]
    public void CaseInsensitive(string input, int expected)
        => Assert.Equal(expected, RomanNumeralConverter.Convert(input));

    [Fact]
    public void InvalidChars_ReturnsNull()
        => Assert.Null(RomanNumeralConverter.Convert("ABC"));

    [Fact]
    public void EmptyString_ReturnsZero()
        => Assert.Equal(0, RomanNumeralConverter.Convert(""));

    [Fact]
    public void Null_ReturnsZero()
        => Assert.Equal(0, RomanNumeralConverter.Convert(null));
}
