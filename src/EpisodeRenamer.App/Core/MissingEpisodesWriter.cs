using System.IO;
using System.Text;

namespace EpisodeRenamer.Core;

public static class MissingEpisodesWriter
{
    private static readonly string FileName = "\u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629.txt";

    public static string? WriteFile(string? path, MissingAnalysis analysis)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (!Directory.Exists(path)) return null;

        string targetFile = Path.Combine(path, FileName);

        if (analysis.Count <= 0)
        {
            if (File.Exists(targetFile))
                File.Delete(targetFile);
            return null;
        }

        List<string> lines = new()
        {
            "=== \u0645\u0644\u0641 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629 ===",
            $"\u0627\u0644\u062a\u0627\u0631\u064a\u062e: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"\u0645\u062c\u0644\u062f \u0627\u0644\u062d\u0644\u0642\u0627\u062a: {path}",
            "",
            $"\u0639\u062f\u062f \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629: {analysis.Count}",
            "",
            "\u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629 \u0628\u0627\u0644\u062a\u0641\u0635\u064a\u0644:",
            "------------------------",
        };

        foreach (var run in analysis.Runs)
        {
            if (run.Start == run.End)
                lines.Add($"\u2022 \u0627\u0644\u062d\u0644\u0642\u0629 {run.Start} \u0645\u0641\u0642\u0648\u062f\u0629.");
            else
                lines.Add($"\u2022 \u0646\u0642\u0635 \u0645\u0646 \u0627\u0644\u062d\u0644\u0642\u0629 {run.Start} \u0625\u0644\u0649 \u0627\u0644\u062d\u0644\u0642\u0629 {run.End}.");
        }

        lines.Add("");
        lines.Add("=== \u0646\u0647\u0627\u064a\u0629 \u0627\u0644\u0645\u0644\u0641 ===");

        File.WriteAllLines(targetFile, lines, new UTF8Encoding(true));
        return targetFile;
    }
}
