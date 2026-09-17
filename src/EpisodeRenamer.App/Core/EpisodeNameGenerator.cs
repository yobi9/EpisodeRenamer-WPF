using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EpisodeRenamer.Core;

public static class EpisodeNameGenerator
{
    private static readonly RegexOptions IC = RegexOptions.IgnoreCase;

    private static readonly Regex MultiEpisodeAdjacent = new(
        @"[Ss]\d{1,2}[\s._-]*[Ee](\d{1,3})[Ee](\d{1,3})(?!\d)", IC);

    private static readonly Regex MultiEpisodeRange = new(
        @"(?<!\d)(\d{1,3})\s*(?:[-+&,]|\u0648)\s*[Ee]?(\d{1,3})(?!\d)", IC);

    public static int? GetSeasonNumber(FileInfo file, string? rootPath)
    {
        string nameLat = TextNormalizer.Normalize(file.Name);
        var m = Regex.Match(nameLat, @"[sS][\s._-]*([0-9]+)[\s._-]?[eE][\s._-]*([0-9]+)", IC);
        if (m.Success) return int.Parse(m.Groups[1].Value);

        string root = (rootPath ?? "").TrimEnd('\\');
        DirectoryInfo? dir = file.Directory;
        bool first = true;
        while (dir is not null)
        {
            int? s = SeasonResolver.Resolve(dir.Name, allowGeneric: first);
            if (s is not null) return s;
            first = false;
            if (string.Equals(dir.FullName.TrimEnd('\\'), root, StringComparison.OrdinalIgnoreCase))
                break;
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>
    /// Extracts only the (first) episode number of a file, without generating a name.
    /// Used by the engine to order files numerically.
    /// </summary>
    public static int? GetEpisodeNumber(FileInfo file, string? rootPath)
    {
        string nameLat = TextNormalizer.Normalize(file.Name);
        string nameBase = TextNormalizer.Normalize(Path.GetFileNameWithoutExtension(file.Name));

        var range = ExtractEpisodeRange(Path.GetFileNameWithoutExtension(file.Name));
        if (range.First is not null) return range.First;

        return ExtractSingleEpisode(nameLat, nameBase);
    }

    public static (int? Episode, string? NewName) GenerateNewName(
        FileInfo file, string? rootPath, string style, string? showName, bool cleanTags,
        string? customPattern = null)
    {
        var (ep, _, name) = GenerateNewNameInfo(file, rootPath, style, showName, cleanTags, customPattern);
        return (ep, name);
    }

    /// <summary>
    /// Generates a new file name, optionally carrying a second (combined) episode.
    /// When a second episode is detected it is formatted with the exact same style
    /// as the first one and joined with a hyphen, e.g. "S01E055-S01E056".
    /// </summary>
    public static (int? Episode, int? EpisodeEnd, string? NewName) GenerateNewNameInfo(
        FileInfo file, string? rootPath, string style, string? showName, bool cleanTags,
        string? customPattern = null)
    {
        string nameLat = TextNormalizer.Normalize(file.Name);
        string nameBase = TextNormalizer.Normalize(Path.GetFileNameWithoutExtension(file.Name));

        int season = GetSeasonNumber(file, rootPath) ?? 1;

        int? ep;
        int? epEnd;
        var range = ExtractEpisodeRange(Path.GetFileNameWithoutExtension(file.Name));
        if (range.First is not null)
        {
            ep = range.First;
            epEnd = range.Second;
        }
        else
        {
            ep = ExtractSingleEpisode(nameLat, nameBase);
            epEnd = null;
        }

        if (ep is null) return (null, null, null);

        bool isCustom = !string.IsNullOrWhiteSpace(customPattern);
        string pattern = isCustom
            ? NameFormatter.FormatCustomTemplate(season, ep.Value, epEnd, showName?.Trim(), customPattern)
            : NameFormatter.Format(season, ep.Value, epEnd, style);
        string show = showName?.Trim() ?? string.Empty;

        if (isCustom)
        {
            string trimmed = pattern.Trim();
            return (ep, epEnd, trimmed.Length > 0 ? $"{trimmed}{file.Extension}" : $"{file.Extension}");
        }

        if (show.Length > 0)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                show = show.Replace(c.ToString(), string.Empty);

            show = show.TrimEnd(' ', '.');
            show = Regex.Replace(show, @"^[-\.\s]+", string.Empty);

            if (cleanTags)
            {
                foreach (string p in TextNormalizer.SourceTagPatterns)
                    show = Regex.Replace(show, p, string.Empty, IC);
                show = Regex.Replace(show, @"[\s_]+", " ");
                show = Regex.Replace(show, @"\s+", " ");
                show = Regex.Replace(show, @"\s*-\s*", "-");
                show = Regex.Replace(show, @"^[\s-]+|[\s-]+$", string.Empty);
            }

            if (show.Length > 0)
                return (ep, epEnd, $"{show}-{pattern}{file.Extension}");
            return (ep, epEnd, $"{pattern}{file.Extension}");
        }

        return (ep, epEnd, $"{pattern}{file.Extension}");
    }

    private static (int? First, int? Second) ExtractEpisodeRange(string rawBase)
    {
        var m = MultiEpisodeAdjacent.Match(rawBase);
        if (m.Success)
        {
            int a = int.Parse(m.Groups[1].Value);
            int b = int.Parse(m.Groups[2].Value);
            if (b > a) return (a, b);
        }

        m = MultiEpisodeRange.Match(rawBase);
        if (m.Success)
        {
            int a = int.Parse(m.Groups[1].Value);
            int b = int.Parse(m.Groups[2].Value);
            if (b > a) return (a, b);
        }

        return (null, null);
    }

    private static int? ExtractSingleEpisode(string nameLat, string nameBase)
    {
        Match m;
        if ((m = Regex.Match(nameLat, @"[sS](\d+)[\.\-\s]?[eE](\d+)", IC)).Success)
            return int.Parse(m.Groups[2].Value);
        if ((m = Regex.Match(nameLat, @"(\d+)x(\d+)", IC)).Success)
            return int.Parse(m.Groups[2].Value);
        if ((m = Regex.Match(nameLat, @"(?:Episode|EP)[\s._-]*(\d+)", IC)).Success)
            return int.Parse(m.Groups[1].Value);
        if ((m = Regex.Match(nameLat, @"(?:\u0627\u0644\u062d\u0644\u0642\u0629|\u062d\u0644\u0642\u0629)[\s._-]*(\d+)")).Success)
            return int.Parse(m.Groups[1].Value);
        if ((m = Regex.Match(nameBase, @"^(\d{1,3})$")).Success)
            return int.Parse(m.Groups[1].Value);
        if ((m = Regex.Match(nameBase, @"^(\d{1,3})(?!\d)")).Success)
            return int.Parse(m.Groups[1].Value);
        if ((m = Regex.Match(nameLat, @"(?:[_\s.\-])(\d{1,3})(?!\d)")).Success)
            return int.Parse(m.Groups[1].Value);
        return null;
    }
}
