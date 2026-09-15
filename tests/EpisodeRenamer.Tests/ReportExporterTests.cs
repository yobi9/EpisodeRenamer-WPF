using System.IO;
using EpisodeRenamer.Core;
using Xunit;

namespace EpisodeRenamer.Tests;

public class ReportExporterTests
{
    private readonly string _dir = TestHelpers.NewTempDir();

    [Fact]
    public void BuildLines_PreviewMode_ContainsPreviewHeaders()
    {
        var stats = new RunStats(Mapped: 3, Correct: 1, Ignored: 2, Missing: 2);
        var runs = new List<(int, int)> { (3, 4), (6, 6) };
        var log = new List<ReportItem>
        {
            new("\u0645\u0639\u062f\u0644", null, "a.S01E01.mkv", "Show-S01E01.mkv", null),
            new("\u0635\u062d\u064a\u062d", "Show-S01E02.mkv", "Show-S01E02.mkv", "Show-S01E02.mkv", null),
            new("\u0645\u062a\u062c\u0627\u0647\u0644", "subs.srt", null, null, "\u0644\u064a\u0633 \u0645\u0644\u0641 \u0641\u064a\u062f\u064a\u0648")
        };

        List<string> lines = ReportExporter.BuildLines(ReportExporter.ModePreview, stats, runs, log);

        string all = string.Join("\n", lines);
        Assert.Contains("=== \u062a\u0642\u0631\u064a\u0631 \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629", all);
        Assert.Contains("(\u0648\u0636\u0639 \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629", all);
        Assert.Contains("\u0633\u064a\u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0627\u0644\u0627\u0633\u0645 \u0644\u0639\u062f\u062f: 3", all);
        Assert.Contains("\u0623\u0633\u0645\u0627\u0621 \u0635\u062d\u064a\u062d\u0629 \u0628\u0627\u0644\u0641\u0639\u0644 \u0648\u0644\u0646 \u062a\u0644\u0645\u0633: 1", all);
        Assert.Contains("[\u0633\u064a\u062a\u0645 \u0627\u0644\u062a\u0639\u062f\u064a\u0644]  a.S01E01.mkv -> Show-S01E01.mkv", all);
        Assert.Contains("[\u0633\u064a\u062a\u0645 \u0627\u0644\u062a\u062c\u0627\u0647\u0644]  subs.srt", all);
        Assert.Contains("\u0646\u0642\u0635 \u0645\u0646 \u0627\u0644\u062d\u0644\u0642\u0629 3 \u0625\u0644\u0649 \u0627\u0644\u062d\u0644\u0642\u0629 4", all);
        Assert.Contains("\u2022 \u0627\u0644\u062d\u0644\u0642\u0629 6 \u0645\u0641\u0642\u0648\u062f\u0629.", all);
    }

    [Fact]
    public void BuildLines_ExecuteMode_UsesExecutedHeaders()
    {
        var stats = new RunStats(Mapped: 2, Correct: 0, Ignored: 0, Missing: 0);
        var log = new List<ReportItem>
        {
            new("\u0645\u0639\u062f\u0644", null, "a.mkv", "b.mkv", null)
        };

        List<string> lines = ReportExporter.BuildLines(ReportExporter.ModeExecute, stats, new List<(int, int)>(), log);

        string all = string.Join("\n", lines);
        Assert.Contains("=== \u062a\u0642\u0631\u064a\u0631 \u0625\u0639\u0627\u062f\u0629 \u062a\u0633\u0645\u064a\u0629 \u0627\u0644\u062d\u0644\u0642\u0627\u062a ===", all);
        Assert.Contains("(\u062a\u0645 \u062a\u0646\u0641\u064a\u0630 \u0639\u0645\u0644\u064a\u0629 \u0625\u0639\u0627\u062f\u0629 \u0627\u0644\u062a\u0633\u0645\u064a\u0629 \u0641\u0639\u0644\u064a\u0627\u064b)", all);
        Assert.Contains("[\u062a\u0645 \u0627\u0644\u062a\u0639\u062f\u064a\u0644]  a.mkv -> b.mkv", all);
        Assert.Contains("\u2022 \u0644\u0627 \u062a\u0648\u062c\u062f \u062d\u0644\u0642\u0627\u062a \u0645\u0641\u0642\u0648\u062f\u0629.", all);
    }

    [Fact]
    public void BuildLines_NoStats_PrintsNoData()
    {
        List<string> lines = ReportExporter.BuildLines(ReportExporter.ModePreview, null, new List<(int, int)>(), new List<ReportItem>());
        string all = string.Join("\n", lines);
        Assert.Contains("\u2022 \u0644\u0627 \u062a\u0648\u062c\u062f \u0628\u064a\u0627\u0646\u0627\u062a \u0628\u0639\u062f", all);
        Assert.Contains("\u2022 \u0644\u0627 \u062a\u0648\u062c\u062f \u0639\u0645\u0644\u064a\u0627\u062a \u0645\u0633\u062c\u0644\u0629.", all);
    }

    [Fact]
    public void WriteReport_WritesUtf8Bom()
    {
        string file = Path.Combine(_dir, "report.txt");
        ReportExporter.WriteReport(file, new List<string> { "hello", "world" });

        byte[] bytes = File.ReadAllBytes(file);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        string text = File.ReadAllText(file);
        Assert.Contains("hello", text);
        Assert.Contains("world", text);
    }

    [Fact]
    public void GetDefaultFileName_ContainsModeAndDate()
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd");
        Assert.Contains(date, ReportExporter.GetDefaultFileName(ReportExporter.ModePreview));
        Assert.Contains("\u0645\u0639\u0627\u064a\u0646\u0629", ReportExporter.GetDefaultFileName(ReportExporter.ModePreview));
        Assert.Contains("\u0625\u0639\u0627\u062f\u0629-\u062a\u0633\u0645\u064a\u0629", ReportExporter.GetDefaultFileName(ReportExporter.ModeExecute));
        Assert.EndsWith(".txt", ReportExporter.GetDefaultFileName(ReportExporter.ModeExecute));
    }
}