namespace EpisodeRenamer.Core;

public static class NameFormatter
{
    public static string Format(int season, int episode, string? style)
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
    {
        if (string.IsNullOrWhiteSpace(template))
            return Format(season, episode, null);

        string result = template;
        result = result.Replace("{show}", showName ?? string.Empty);
        result = result.Replace("{season}", season.ToString(System.Globalization.CultureInfo.InvariantCulture));
        result = result.Replace("{ep}", episode.ToString(System.Globalization.CultureInfo.InvariantCulture));
        result = result.Replace("{ep2}", episode.ToString().PadLeft(2, '0'));
        result = result.Replace("{ep3}", episode.ToString().PadLeft(3, '0'));
        return result;
    }
}
