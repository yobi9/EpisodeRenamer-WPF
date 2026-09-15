using System.Text.RegularExpressions;

namespace EpisodeRenamer.Core;

public static class TextNormalizer
{
    private static readonly (string From, string To)[] ArabicToLatinDigits =
    {
        ("٠", "0"), ("١", "1"), ("٢", "2"), ("٣", "3"), ("٤", "4"),
        ("٥", "5"), ("٦", "6"), ("٧", "7"), ("٨", "8"), ("٩", "9"),
    };

    public static readonly string[] SourceTagPatterns =
    {
        @"\[[^\]]*\]", "مدبلج", "مترجم", "960p", "720p", "1080p", "4K",
        "WEB-?DL", "BluRay", @"\bHD\b",
    };

    public static string ConvertLatinDigits(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        foreach (var (from, to) in ArabicToLatinDigits)
            text = text.Replace(from, to);
        return text;
    }

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        text = ConvertLatinDigits(text);
        text = Regex.Replace(text, "\u0640", string.Empty);
        text = Regex.Replace(text, @"[._\-–—\[\]()]+", " ");
        text = Regex.Replace(text, @"\s{2,}", " ");
        return text.Trim();
    }

    public static string RemoveSourceTags(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        foreach (var pattern in SourceTagPatterns)
            text = Regex.Replace(text, pattern, string.Empty);
        text = Regex.Replace(text, @"[\s_-]+", " ");
        text = Regex.Replace(text, @"^[\s-]+|[\s-]+$", string.Empty);
        return text;
    }
}