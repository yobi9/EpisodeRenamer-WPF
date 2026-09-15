using System.IO;
using System.Text.RegularExpressions;

namespace EpisodeRenamer.Core;

public static class ShowNameDetector
{
    private static readonly string RemovePatterns = @"\[[^\]]*\]|مدبلج|مترجم|960p|720p|1080p|4K|WEB-?DL|BluRay|\bHD\b";

    public static string Detect(string? path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;

        string leaf = Path.GetFileName(path.TrimEnd('\\', '/'));
        if (string.IsNullOrEmpty(leaf)) return string.Empty;

        string n = TextNormalizer.Normalize(leaf);
        if (string.IsNullOrEmpty(n)) return string.Empty;

        n = Regex.Replace(n, @"\[[^\]]*\]", " ");
        n = Regex.Replace(n, @"[\s_]+", " ").Trim();
        n = Regex.Replace(n, @"(?i)\s*(Season\s*\d+|S\d+\s*E\d+|S\d+)[\s.,-]*$", string.Empty);
        n = Regex.Replace(
            n,
            @"(?:\s+|^)(\u0627\u0644\u0645\u0648\u0633\u0645\s*(?:\u0627\u0644\u0623\u0648\u0644|\u0627\u0644\u062b\u0627\u0646\u064a|\u0627\u0644\u062b\u0627\u0644\u062b|\u0627\u0644\u0631\u0627\u0628\u0639|\u0627\u0644\u062e\u0627\u0645\u0633|\u0627\u0644\u0633\u0627\u062f\u0633|\u0627\u0644\u0633\u0627\u0628\u0639|\u0627\u0644\u062b\u0627\u0645\u0646|\u0627\u0644\u062a\u0627\u0633\u0639|\u0627\u0644\u0639\u0627\u0634\u0631|[\u0660-\u0669\d]+))[\s.,-]*$",
            string.Empty,
            RegexOptions.IgnoreCase);

        return n.Trim(' ', '.', '_', '-');
    }
}
