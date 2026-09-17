using System.Globalization;

namespace EpisodeRenamer.Core;

public static class NameFormatter
{
    public static string Format(int season, int episode, string? style)
        => FormatEpisode(season, episode, style);

    /// <summary>
    /// Formats an episode, and when <paramref name="secondEpisode"/> is provided,
    /// formats it with the exact same style and joins the two with a hyphen
    /// (for example "S01E055-S01E056").
    /// </summary>
    public static string Format(int season, int episode, int? secondEpisode, string? style)
    {
        string first = FormatEpisode(season, episode, style);
        if (secondEpisode is null) return first;
        return $"{first}-{FormatEpisode(season, secondEpisode.Value, style)}";
    }

    private static string FormatEpisode(int season, int episode, string? style)
    {
        string s = season.ToString().PadLeft(2, '0');
        string e3 = episode.ToString().PadLeft(3, '0');
        if (string.IsNullOrEmpty(style)) return $"S{s}E{e3}";

        if (style.StartsWith("S01E01", StringComparison.Ordinal)) return $"S{s}E{e3}";
        if (style.StartsWith("S01.E01", StringComparison.Ordinal)) return $"S{s}.E{e3}";
        if (style.StartsWith("Season 01 Episode 01", StringComparison.Ordinal))
        {
            string e2 = episode.ToString().PadLeft(2, '0');
            return $"Season {s} Episode {e2}";
        }
        if (style.StartsWith("EP 001", StringComparison.Ordinal)) return $"EP {e3}";
        if (style.StartsWith("#01", StringComparison.Ordinal))
        {
            string e2 = episode.ToString().PadLeft(2, '0');
            return $"#{e2}";
        }
        if (style.StartsWith("\u0627\u0644\u0645\u0648\u0633\u0645 01 - \u0627\u0644\u062d\u0644\u0642\u0629 01", StringComparison.Ordinal))
        {
            string e2 = episode.ToString().PadLeft(2, '0');
            return $"\u0627\u0644\u0645\u0648\u0633\u0645 {s} \u0627\u0644\u062d\u0644\u0642\u0629 {e2}";
        }
        if (style.StartsWith("S1E1", StringComparison.Ordinal)) return $"S{season}E{episode}";
        if (style.StartsWith("EP01", StringComparison.Ordinal))
        {
            string e2 = episode.ToString().PadLeft(2, '0');
            return $"EP{e2}";
        }
        return $"S{s}E{e3}";
    }

    public static string FormatCustomTemplate(int season, int episode, string? showName, string? template)
        => FormatCustomTemplate(season, episode, null, showName, template);

    /// <summary>
    /// Applies a custom template. The second episode can be used through the
    /// {epEnd}, {epEnd2} and {epEnd3} tokens. When the template uses none of them
    /// yet a second episode exists, the template is applied to both episodes and
    /// the results are joined with a hyphen.
    /// </summary>
    public static string FormatCustomTemplate(
        int season, int episode, int? secondEpisode, string? showName, string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
            return Format(season, episode, secondEpisode, null);

        bool hasEndToken =
            template.Contains("{epEnd}") || template.Contains("{epEnd2}") || template.Contains("{epEnd3}");

        if (secondEpisode is null || hasEndToken)
            return ApplyTemplate(template, season, episode, secondEpisode, showName);

        return ApplyTemplate(template, season, episode, null, showName)
            + "-" + ApplyTemplate(template, season, secondEpisode.Value, null, showName);
    }

    private static string ApplyTemplate(string template, int season, int episode, int? secondEpisode, string? showName)
    {
        string endRaw = secondEpisode?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        string end2 = endRaw.Length > 0 ? endRaw.PadLeft(2, '0') : string.Empty;
        string end3 = endRaw.Length > 0 ? endRaw.PadLeft(3, '0') : string.Empty;

        string result = template;
        result = result.Replace("{show}", showName ?? string.Empty);
        result = result.Replace("{season}", season.ToString(CultureInfo.InvariantCulture));
        result = result.Replace("{ep}", episode.ToString(CultureInfo.InvariantCulture));
        result = result.Replace("{ep2}", episode.ToString().PadLeft(2, '0'));
        result = result.Replace("{ep3}", episode.ToString().PadLeft(3, '0'));
        result = result.Replace("{epEnd}", endRaw);
        result = result.Replace("{epEnd2}", end2);
        result = result.Replace("{epEnd3}", end3);
        return result;
    }
}
