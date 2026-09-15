using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace EpisodeRenamer.Core;

public sealed class FileProcessingEngine
{
    private readonly UndoLogManager _undoLog;

    public FileProcessingEngine(UndoLogManager undoLog)
    {
        _undoLog = undoLog;
    }

    public RunResult Run(
        string? path,
        bool previewOnly,
        bool recurse,
        string style,
        string? showName,
        bool cleanTags,
        Action<int, int>? progress = null)
    {
        RunResult result = new()
        {
            Mode = previewOnly ? "\u0645\u0639\u0627\u064a\u0646\u0629" : "\u062a\u0646\u0641\u064a\u0630"
        };
        List<string> out_ = result.OutputLines;
        void L(string s) => out_.Add(s);

        List<int> episodeNumbers = new();
        string realPath = path?.Trim() ?? string.Empty;

        if (!Directory.Exists(realPath))
        {
            L("❌ \u0627\u0644\u0645\u0633\u0627\u0631 \u063a\u064a\u0631 \u0635\u0627\u0644\u062d");
            return result;
        }

        if (previewOnly)
        {
            L("");
            L("👁️ \u0648\u0636\u0639 \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629: \u0647\u0630\u0647 \u0634\u0627\u0634\u0629 \u0627\u0633\u062a\u0643\u0634\u0627\u0641 \u0641\u0642\u0637\u060c \u0648\u0644\u0646 \u064a\u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0623\u064a \u0645\u0644\u0641\u0627\u062a \u0627\u0644\u0622\u0646");
            L("--------------------------------------------");
        }
        else
        {
            L("");
            L("✍️ \u0648\u0636\u0639 \u0625\u0639\u0627\u062f\u0629 \u0627\u0644\u062a\u0633\u0645\u064a\u0629: \u0633\u064a\u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0623\u0633\u0645\u0627\u0621 \u0627\u0644\u0645\u0644\u0641\u0627\u062a \u0641\u0639\u0644\u064a\u0627\u064b \u0627\u0644\u0622\u0646");
            L("--------------------------------------------");
        }

        DiscoveryResult discovery = FileDiscovery.Discover(realPath, recurse);

        foreach (FileInfo ign in discovery.Ignored)
            result.Log.Add(new ReportItem("\u0645\u062a\u062c\u0627\u0647\u0644", ign.Name, null, null, "\u0644\u064a\u0633 \u0645\u0644\u0641 \u0641\u064a\u062f\u064a\u0648"));

        List<FileInfo> files = discovery.Video
            .OrderBy(f => GetSeasonNumber(f, realPath) ?? int.MaxValue)
            .ToList();

        Dictionary<string, string> targetSeen = new(StringComparer.OrdinalIgnoreCase);
        int cntMap = 0;
        int cntCorrect = 0;
        int cntExists = 0;
        Stopwatch sw = Stopwatch.StartNew();
        int total = files.Count;
        int current = 0;

        if (!previewOnly) _undoLog.Clear();

        int lastSeason = -1;
        foreach (FileInfo file in files)
        {
            int? seasonH = GetSeasonNumber(file, realPath);
            if (seasonH != lastSeason)
            {
                lastSeason = seasonH ?? -1;
                if (seasonH is null)
                {
                    L("");
                    L("==================== \u0627\u0644\u0645\u0648\u0633\u0645 (\u063a\u064a\u0631 \u0645\u062d\u062f\u062f) ====================");
                }
                else
                {
                    string s2 = seasonH.Value.ToString().PadLeft(2, '0');
                    L($"==================== \u0627\u0644\u0645\u0648\u0633\u0645 {s2} (Season {s2}) ====================");
                }
                L(previewOnly
                    ? "\u0627\u0644\u0645\u0644\u0641\u0627\u062a \u0627\u0644\u062a\u064a \u0633\u064a\u062a\u0645 \u062a\u063a\u064a\u064a\u0631 \u0627\u0633\u0645\u0647\u0627:"
                    : "\u0627\u0644\u0645\u0644\u0641\u0627\u062a \u0627\u0644\u062a\u064a \u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0627\u0633\u0645\u0647\u0627:");
                L("--------------------------------------------");
            }

            var (ep, newName) = EpisodeNameGenerator.GenerateNewName(file, realPath, style, showName, cleanTags);

            if (newName is not null)
            {
                episodeNumbers.Add(ep ?? 0);
                string targetPath = Path.Combine(file.DirectoryName ?? "", newName);

                if (string.Equals(newName, file.Name, StringComparison.Ordinal))
                {
                    targetSeen[targetPath] = file.Name;
                    L($"✅ {file.Name} — \u0627\u0644\u0627\u0633\u0645 \u0635\u062d\u064a\u062d \u0628\u0627\u0644\u0641\u0639\u0644");
                    result.Log.Add(new ReportItem("\u0635\u062d\u064a\u062d", file.Name, file.Name, newName, null));
                    cntCorrect++;
                }
                else if (targetSeen.ContainsKey(targetPath) || IsExists(targetPath))
                {
                    result.Log.Add(new ReportItem("\u0645\u062a\u062c\u0627\u0647\u0644", file.Name, null, null, "\u064a\u0648\u062c\u062f \u0645\u0644\u0641 \u0628\u0646\u0641\u0633 \u0627\u0644\u0627\u0633\u0645 \u0645\u0633\u0628\u0642\u0627\u064b"));
                    cntExists++;
                }
                else
                {
                    targetSeen[targetPath] = file.Name;
                    L($"🔄 \u0645\u0646: {file.Name} \u25c4 \u0625\u0644\u0649: {newName}");
                    result.Log.Add(new ReportItem("\u0645\u0639\u062f\u0644", null, file.Name, newName, null));
                    cntMap++;

                    if (!previewOnly)
                    {
                        string oldPath = file.FullName;
                        try
                        {
                            File.Move(oldPath, targetPath);
                            _undoLog.Add(new RenameEntry(oldPath, targetPath));
                        }
                        catch
                        {
                            L($"❌ \u0641\u0634\u0644\u062a \u0625\u0639\u0627\u062f\u0629 \u0627\u0644\u062a\u0633\u0645\u064a\u0629: {file.Name}");
                            result.Log.Add(new ReportItem("\u0645\u062a\u062c\u0627\u0647\u0644", file.Name, null, null, "\u0641\u0634\u0644\u062a \u0639\u0645\u0644\u064a\u0629 \u0625\u0639\u0627\u062f\u0629 \u0627\u0644\u062a\u0633\u0645\u064a\u0629"));
                        }
                    }
                }
            }
            else
            {
                result.Log.Add(new ReportItem("\u0645\u062a\u062c\u0627\u0647\u0644", file.Name, null, null, "\u0644\u0645 \u064a\u0639\u062b\u0631 \u0639\u0644\u0649 \u0631\u0642\u0645 \u062d\u0644\u0642\u0629"));
            }

            current++;
            progress?.Invoke(current, total);
        }

        List<ReportItem> ignoredItems = result.Log.Where(i => i.Type == "\u0645\u062a\u062c\u0627\u0647\u0644").ToList();
        L("");
        L(previewOnly ? "\u0645\u0644\u0641\u0627\u062a \u0633\u064a\u062a\u0645 \u062a\u062c\u0627\u0647\u0644\u0647\u0627 (\u0644\u0646 \u062a\u062a\u063a\u064a\u0631):" : "\u0645\u0644\u0641\u0627\u062a \u062a\u0645 \u062a\u062c\u0627\u0647\u0644\u0647\u0627 (\u0644\u0645 \u062a\u062a\u063a\u064a\u0631):");
        L("--------------------------------------------");
        if (ignoredItems.Count > 0)
        {
            foreach (ReportItem ig in ignoredItems)
                L($"⚪ {ig.Name} \u25c4 (\u0627\u0644\u0633\u0628\u0628: {ig.Reason})");
        }
        else
        {
            L("⚪ \u0644\u0627 \u062a\u0648\u062c\u062f \u0645\u0644\u0641\u0627\u062a \u0645\u062a\u062c\u0627\u0647\u0644\u0629");
        }

        sw.Stop();
        string seconds = (sw.Elapsed.TotalSeconds).ToString("0.0", CultureInfo.InvariantCulture);

        if (previewOnly)
        {
            L("");
            L("📊 \u0645\u0644\u062e\u0635 \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629 (\u0644\u0645 \u064a\u062a\u0645 \u062a\u0646\u0641\u064a\u0630 \u0623\u064a \u062a\u063a\u064a\u064a\u0631):");
            L("--------------------------------------------");
            L($"✨ \u0633\u064a\u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0627\u0644\u0627\u0633\u0645 \u0644\u0639\u062f\u062f: {cntMap} \u0645\u0644\u0641");
            if (cntCorrect > 0)
                L($"✅ \u0623\u0633\u0645\u0627\u0621 \u0635\u062d\u064a\u062d\u0629 \u0628\u0627\u0644\u0641\u0639\u0644 \u0648\u0644\u0646 \u062a\u0644\u0645\u0633: {cntCorrect} \u0645\u0644\u0641");
            if (ignoredItems.Count > 0)
                L($"⚪ \u0633\u064a\u062a\u0645 \u062a\u062c\u0627\u0647\u0644\u0647\u0627 \u0648\u062a\u062e\u0637\u064a\u0647\u0627: {ignoredItems.Count} \u0645\u0644\u0641\u0627\u062a");
            L($"⏱️ \u0645\u062f\u0629 \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629: {seconds} \u062b\u0627\u0646\u064a\u0629");
        }
        else
        {
            L("");
            L("📊 \u0645\u0644\u062e\u0635 \u0627\u0644\u0639\u0645\u0644\u064a\u0629:");
            L("--------------------------------------------");
            L($"✨ \u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0627\u0644\u0627\u0633\u0645 \u0628\u0646\u062c\u0627\u062d: {cntMap} \u0645\u0644\u0641\u0627\u064b");
            if (cntCorrect > 0)
                L($"✅ \u0623\u0633\u0645\u0627\u0621 \u0635\u062d\u064a\u062d\u0629 \u0628\u0627\u0644\u0641\u0639\u0644 (\u0644\u0645 \u062a\u063a\u064a\u0651\u0631): {cntCorrect} \u0645\u0644\u0641\u0627\u064b");
            L($"⚪ \u062a\u0645 \u062a\u062c\u0627\u0647\u0644\u0647\u0627 \u0648\u062a\u062e\u0637\u064a\u0647\u0627: {ignoredItems.Count} \u0645\u0644\u0641\u0627\u062a");
            L($"⏱️ \u0627\u0644\u0648\u0642\u062a \u0627\u0644\u0645\u0633\u062a\u063a\u0631\u0642: {seconds} \u062b\u0627\u0646\u064a\u0629");
        }

        MissingAnalysis analysis = GapAnalyzer.Analyze(episodeNumbers);
        result.MissingRuns.AddRange(analysis.Runs);

        if (analysis.Count > 0)
        {
            L("");
            L($"⚠️ \u062a\u0646\u0628\u064a\u0647 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629 ({analysis.Count} \u062d\u0644\u0642\u0629 \u0645\u0641\u0642\u0648\u062f\u0629):");
            L("--------------------------------------------");
            L("\u064a\u0648\u062c\u062f \u0646\u0642\u0635 \u0641\u064a \u062a\u0633\u0644\u0633\u0644 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u062f\u0627\u062e\u0644 \u0627\u0644\u0645\u062c\u0644\u062f\u060c \u0644\u0645 \u064a\u062a\u0645 \u0627\u0644\u0639\u062b\u0648\u0631 \u0639\u0644\u0649:");
            int shown = 0;
            foreach (var run in analysis.Runs)
            {
                if (shown >= 15)
                {
                    L($"... ({analysis.Runs.Count - shown} \u0646\u0637\u0627\u0642\u0627\u062a \u0623\u062e\u0631\u0649)");
                    break;
                }
                if (run.Start == run.End)
                    L($"\u2022 \u0627\u0644\u062d\u0644\u0642\u0629 {run.Start}");
                else
                    L($"\u2022 \u0645\u0646 \u0627\u0644\u062d\u0644\u0642\u0629 {run.Start} \u0625\u0644\u0649 {run.End}");
                shown++;
            }
        }
        else
        {
            L("");
            L("✅ \u0644\u0627 \u062a\u0648\u062c\u062f \u062d\u0644\u0642\u0627\u062a \u0645\u0641\u0642\u0648\u062f\u0629");
        }

        string? savedFile = MissingEpisodesWriter.WriteFile(realPath, analysis);
        if (savedFile is not null)
        {
            L("");
            L("💾 \u062a\u0645 \u062d\u0641\u0638 \u0645\u0644\u0641 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629:");
            L(savedFile);
        }

        result.Stats = new RunStats(cntMap, cntCorrect, ignoredItems.Count, analysis.Count);
        result.MissingFile = savedFile;
        return result;
    }

    private static int? GetSeasonNumber(FileInfo file, string rootPath)
    {
        return EpisodeNameGenerator.GetSeasonNumber(file, rootPath);
    }

    private static bool IsExists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }
}