using System.IO;
using System.Text;

namespace EpisodeRenamer.Core;

public static class MissingEpisodesWriter
{
    private static readonly string FileName = "\u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629.txt";

    public static string? WriteFile(string? path, MissingAnalysis analysis)
    {
        List<(int Season, int Start, int End)> runs = new(analysis.Runs.Count);
        foreach (var run in analysis.Runs)
            runs.Add((0, run.Start, run.End));
        return WriteFile(path, runs);
    }

    /// <summary>
    /// Writes the missing-episodes file grouped by season. Keep the overload
    /// without season information for callers that only have a flat analysis.
    /// </summary>
    public static string? WriteFile(string? path, IReadOnlyList<(int Season, int Start, int End)> runsBySeason)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (!Directory.Exists(path)) return null;

        string targetFile = Path.Combine(path, FileName);
        int total = 0;
        foreach (var run in runsBySeason)
            total += Math.Max(0, run.End - run.Start + 1);

        if (total <= 0)
        {
            try
            {
                if (File.Exists(targetFile))
                    File.Delete(targetFile);
            }
            catch
            {
            }
            return null;
        }

        List<string> lines = new()
        {
            "=== \u0645\u0644\u0641 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629 ===",
            $"\u0627\u0644\u062a\u0627\u0631\u064a\u062e: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"\u0645\u062c\u0644\u062f \u0627\u0644\u062d\u0644\u0642\u0627\u062a: {path}",
            "",
            $"\u0639\u062f\u062f \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629: {total}",
            "",
            "\u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629 \u0628\u0627\u0644\u062a\u0641\u0635\u064a\u0644:",
            "------------------------",
        };

        foreach (var group in runsBySeason.GroupBy(r => r.Season).OrderBy(g => g.Key))
        {
            if (group.Key > 0)
            {
                lines.Add("");
                lines.Add($"[{SeasonHeader(group.Key)}]");
            }
            foreach (var run in group)
            {
                if (run.Start == run.End)
                    lines.Add($"\u2022 \u0627\u0644\u062d\u0644\u0642\u0629 {run.Start} \u0645\u0641\u0642\u0648\u062f\u0629.");
                else
                    lines.Add($"\u2022 \u0646\u0642\u0635 \u0645\u0646 \u0627\u0644\u062d\u0644\u0642\u0629 {run.Start} \u0625\u0644\u0649 \u0627\u0644\u062d\u0644\u0642\u0629 {run.End}.");
            }
        }

        lines.Add("");
        lines.Add("=== \u0646\u0647\u0627\u064a\u0629 \u0627\u0644\u0645\u0644\u0641 ===");

        try
        {
            File.WriteAllLines(targetFile, lines, new UTF8Encoding(true));
            return targetFile;
        }
        catch (Exception ex)
        {
            ErrorLogger.Write(ex, "MissingEpisodesWriter");
            return null;
        }
    }

    private static string SeasonHeader(int season)
        => $"\u0627\u0644\u0645\u0648\u0633\u0645 {season.ToString().PadLeft(2, '0')}";
}
