using System.IO;
using System.Text;

namespace EpisodeRenamer.Core;

public static class ReportExporter
{
    public const string ModePreview = "\u0645\u0639\u0627\u064a\u0646\u0629";
    public const string ModeExecute = "\u062a\u0646\u0641\u064a\u0630";

    public static string GetDefaultFileName(string mode)
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd");
        return mode == ModePreview
            ? $"\u062a\u0642\u0631\u064a\u0631-\u0645\u0639\u0627\u064a\u0646\u0629-{date}.txt"
            : $"\u062a\u0642\u0631\u064a\u0631-\u0625\u0639\u0627\u062f\u0629-\u062a\u0633\u0645\u064a\u0629-\u0627\u0644\u062d\u0644\u0642\u0627\u062a-{date}.txt";
    }

    public static List<string> BuildLines(
        string mode,
        RunStats? stats,
        IReadOnlyList<(int Start, int End)> missingRuns,
        IReadOnlyList<ReportItem> log)
    {
        bool preview = mode == ModePreview;
        List<string> lines = new();

        lines.Add(preview
            ? "=== \u062a\u0642\u0631\u064a\u0631 \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629 (\u0644\u0645 \u064a\u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0623\u064a \u0645\u0644\u0641) ==="
            : "=== \u062a\u0642\u0631\u064a\u0631 \u0625\u0639\u0627\u062f\u0629 \u062a\u0633\u0645\u064a\u0629 \u0627\u0644\u062d\u0644\u0642\u0627\u062a ===");
        lines.Add($"\u0627\u0644\u062a\u0627\u0631\u064a\u062e: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        lines.Add("");
        lines.Add(preview
            ? "(\u0648\u0636\u0639 \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629 — \u0644\u0645 \u064a\u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0623\u064a \u0645\u0644\u0641 \u0641\u0639\u0644\u064a\u0627\u064b)"
            : "(\u062a\u0645 \u062a\u0646\u0641\u064a\u0630 \u0639\u0645\u0644\u064a\u0629 \u0625\u0639\u0627\u062f\u0629 \u0627\u0644\u062a\u0633\u0645\u064a\u0629 \u0641\u0639\u0644\u064a\u0627\u064b)");
        lines.Add("");
        lines.Add("\u0625\u062d\u0635\u0627\u0626\u064a\u0627\u062a \u0633\u0631\u064a\u0639\u0629:");
        lines.Add("------------------------");
        if (stats is not null)
        {
            if (preview)
            {
                lines.Add($"\u2022 \u0633\u064a\u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0627\u0644\u0627\u0633\u0645 \u0644\u0639\u062f\u062f: {stats.Mapped} \u0645\u0644\u0641");
                lines.Add($"\u2022 \u0645\u0644\u0641\u0627\u062a \u0633\u064a\u062a\u0645 \u062a\u062c\u0627\u0647\u0644\u0647\u0627: {stats.Ignored} \u0645\u0644\u0641\u0627\u062a");
                lines.Add($"\u2022 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629: {stats.Missing} \u062d\u0644\u0642\u0629");
            }
            else
            {
                lines.Add($"\u2022 \u0625\u062c\u0645\u0627\u0644\u064a \u0627\u0644\u0645\u0644\u0641\u0627\u062a \u0627\u0644\u0645\u0639\u062f\u0644\u0629: {stats.Mapped}");
                lines.Add($"\u2022 \u0625\u062c\u0645\u0627\u0644\u064a \u0627\u0644\u0645\u0644\u0641\u0627\u062a \u0627\u0644\u0645\u062a\u062c\u0627\u0647\u0644\u0629: {stats.Ignored}");
                lines.Add($"\u2022 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629: {stats.Missing} \u062d\u0644\u0642\u0629");
            }
            if (stats.Correct > 0)
            {
                lines.Add(preview
                    ? $"\u2022 \u0623\u0633\u0645\u0627\u0621 \u0635\u062d\u064a\u062d\u0629 \u0628\u0627\u0644\u0641\u0639\u0644 \u0648\u0644\u0646 \u062a\u0644\u0645\u0633: {stats.Correct} \u0645\u0644\u0641\u0627\u062a"
                    : $"\u2022 \u0623\u0633\u0645\u0627\u0621 \u0635\u062d\u064a\u062d\u0629 \u0628\u0627\u0644\u0641\u0639\u0644 (\u0644\u0645 \u062a\u063a\u064a\u0651\u0631): {stats.Correct} \u0645\u0644\u0641\u0627\u062a");
            }
        }
        else
        {
            lines.Add("\u2022 \u0644\u0627 \u062a\u0648\u062c\u062f \u0628\u064a\u0627\u0646\u0627\u062a \u0628\u0639\u062f");
        }
        lines.Add("");
        lines.Add("\u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629 \u0628\u0627\u0644\u062a\u0641\u0635\u064a\u0644:");
        lines.Add("------------------------");
        if (missingRuns.Count > 0)
        {
            foreach (var run in missingRuns)
            {
                if (run.Start == run.End)
                    lines.Add($"\u2022 \u0627\u0644\u062d\u0644\u0642\u0629 {run.Start} \u0645\u0641\u0642\u0648\u062f\u0629.");
                else
                    lines.Add($"\u2022 \u0646\u0642\u0635 \u0645\u0646 \u0627\u0644\u062d\u0644\u0642\u0629 {run.Start} \u0625\u0644\u0649 \u0627\u0644\u062d\u0644\u0642\u0629 {run.End}.");
            }
        }
        else
        {
            lines.Add("\u2022 \u0644\u0627 \u062a\u0648\u062c\u062f \u062d\u0644\u0642\u0627\u062a \u0645\u0641\u0642\u0648\u062f\u0629.");
        }
        lines.Add("");
        lines.Add(preview ? "\u0633\u062c\u0644 \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629 \u0628\u0627\u0644\u062a\u0641\u0635\u064a\u0644:" : "\u0633\u062c\u0644 \u0627\u0644\u0639\u0645\u0644\u064a\u0627\u062a \u0628\u0627\u0644\u062a\u0641\u0635\u064a\u0644:");
        lines.Add("------------------------");
        if (log.Count > 0)
        {
            foreach (ReportItem e in log)
            {
                if (e.Type == "\u0645\u0639\u062f\u0644")
                {
                    lines.Add(preview
                        ? $"[\u0633\u064a\u062a\u0645 \u0627\u0644\u062a\u0639\u062f\u064a\u0644]  {e.Old} -> {e.New}"
                        : $"[\u062a\u0645 \u0627\u0644\u062a\u0639\u062f\u064a\u0644]  {e.Old} -> {e.New}");
                }
                else if (e.Type == "\u0635\u062d\u064a\u062d")
                {
                    lines.Add($"[\u0627\u0644\u0627\u0633\u0645 \u0635\u062d\u064a\u062d]  {e.Name}");
                }
                else
                {
                    lines.Add(preview
                        ? $"[\u0633\u064a\u062a\u0645 \u0627\u0644\u062a\u062c\u0627\u0647\u0644]  {e.Name} ({e.Reason})"
                        : $"[\u062a\u0645 \u0627\u0644\u062a\u062c\u0627\u0647\u0644]  {e.Name} ({e.Reason})");
                }
            }
        }
        else
        {
            lines.Add("\u2022 \u0644\u0627 \u062a\u0648\u062c\u062f \u0639\u0645\u0644\u064a\u0627\u062a \u0645\u0633\u062c\u0644\u0629.");
        }
        lines.Add("");
        lines.Add("=== \u0646\u0647\u0627\u064a\u0629 \u0627\u0644\u062a\u0642\u0631\u064a\u0631 ===");
        return lines;
    }

    public static void WriteReport(string filePath, IReadOnlyList<string> lines)
    {
        File.WriteAllLines(filePath, lines, new UTF8Encoding(true));
    }
}