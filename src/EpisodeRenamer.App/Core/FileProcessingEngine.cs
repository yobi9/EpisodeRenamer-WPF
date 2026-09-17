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

    private static readonly HashSet<string> SubtitleExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "srt", "ass", "ssa", "sub", "vtt"
    };

    public RunResult Run(
        string? path,
        bool previewOnly,
        bool recurse,
        string style,
        string? showName,
        bool cleanTags,
        Action<int, int>? progress = null,
        string? customPattern = null,
        string? ignorePatterns = null,
        bool renameSubtitles = false,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<string, string>? nameOverrides = null)
    {
        RunResult result = new()
        {
            Mode = previewOnly ? "\u0645\u0639\u0627\u064a\u0646\u0629" : "\u062a\u0646\u0641\u064a\u0630"
        };
        List<string> out_ = result.OutputLines;
        void L(string s) => out_.Add(s);

        Dictionary<int, List<int>> seasonEpisodes = new();
        string realPath = path?.Trim() ?? string.Empty;

        if (!Directory.Exists(realPath))
        {
            L("\u274c \u0627\u0644\u0645\u0633\u0627\u0631 \u063a\u064a\u0631 \u0635\u0627\u0644\u062d");
            return result;
        }

        if (previewOnly)
        {
            L("");
            L("\ud83d\udc41\ufe0f \u0648\u0636\u0639 \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629: \u0647\u0630\u0647 \u0634\u0627\u0634\u0629 \u0627\u0633\u062a\u0643\u0634\u0627\u0641 \u0641\u0642\u0637\u060c \u0648\u0644\u0646 \u064a\u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0623\u064a \u0645\u0644\u0641\u0627\u062a \u0627\u0644\u0622\u0646");
            L("--------------------------------------------");
        }
        else
        {
            L("");
            L("\u270d\ufe0f \u0648\u0636\u0639 \u0625\u0639\u0627\u062f\u0629 \u0627\u0644\u062a\u0633\u0645\u064a\u0629: \u0633\u064a\u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0623\u0633\u0645\u0627\u0621 \u0627\u0644\u0645\u0644\u0641\u0627\u062a \u0641\u0639\u0644\u064a\u0627\u064b \u0627\u0644\u0622\u0646");
            L("--------------------------------------------");
        }

        DiscoveryResult discovery = FileDiscovery.Discover(realPath, recurse);

        HashSet<string> ignoreTokens = ParseIgnorePatterns(ignorePatterns);

        foreach (FileInfo ign in discovery.Ignored)
            result.Log.Add(new ReportItem("\u0645\u062a\u062c\u0627\u0647\u0644", ign.Name, null, null, "\u0644\u064a\u0633 \u0645\u0644\u0641 \u0641\u064a\u062f\u064a\u0648"));

        List<FileInfo> files = new();
        foreach (FileInfo f in discovery.Video)
        {
            if (ShouldIgnore(f.Name, ignoreTokens))
            {
                result.Log.Add(new ReportItem("\u0645\u062a\u062c\u0627\u0647\u0644", f.Name, null, null, "\u064a\u0637\u0627\u0628\u0642 \u0646\u0645\u0637 \u0627\u0644\u0627\u0633\u062a\u0628\u0639\u0627\u062f"));
            }
            else
            {
                files.Add(f);
            }
        }

        files = files
            .OrderBy(f => GetSeasonNumber(f, realPath) ?? int.MaxValue)
            .ThenBy(f => EpisodeNameGenerator.GetEpisodeNumber(f, realPath) ?? int.MaxValue)
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
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
            if (cancellationToken.IsCancellationRequested)
            {
                result.Cancelled = true;
                L("");
                L("\u23f9\ufe0f \u062a\u0645 \u0625\u064a\u0642\u0627\u0641 \u0627\u0644\u0639\u0645\u0644\u064a\u0629 \u064a\u062f\u0648\u064a\u0627\u064b");
                break;
            }

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

            var (ep, epEnd, newName) = EpisodeNameGenerator.GenerateNewNameInfo(file, realPath, style, showName, cleanTags, customPattern);

            if (newName is not null && nameOverrides is not null
                && nameOverrides.TryGetValue(file.Name, out string? manualOverride)
                && !string.IsNullOrWhiteSpace(manualOverride))
            {
                string safe = Path.GetFileName(manualOverride);
                if (!string.IsNullOrWhiteSpace(safe))
                    newName = safe;
            }

            if (newName is not null)
            {
                int seasonKey = seasonH ?? 1;
                if (!seasonEpisodes.TryGetValue(seasonKey, out List<int>? seasonEps))
                {
                    seasonEps = new List<int>();
                    seasonEpisodes[seasonKey] = seasonEps;
                }
                seasonEps.Add(ep ?? 0);
                if (epEnd is not null) seasonEps.Add(epEnd.Value);

                string targetPath = Path.Combine(file.DirectoryName ?? "", newName);

                if (string.Equals(newName, file.Name, StringComparison.Ordinal))
                {
                    targetSeen[targetPath] = file.Name;
                    L($"\u2705 {file.Name} \u2014 \u0627\u0644\u0627\u0633\u0645 \u0635\u062d\u064a\u062d \u0628\u0627\u0644\u0641\u0639\u0644");
                    result.Log.Add(new ReportItem("\u0635\u062d\u064a\u062d", file.Name, file.Name, newName, null) { Season = seasonH, Episode = ep });
                    cntCorrect++;
                }
                else if (targetSeen.ContainsKey(targetPath) || IsExists(targetPath))
                {
                    result.Log.Add(new ReportItem("\u0645\u062a\u062c\u0627\u0647\u0644", file.Name, null, null, "\u064a\u0648\u062c\u062f \u0645\u0644\u0641 \u0628\u0646\u0641\u0633 \u0627\u0644\u0627\u0633\u0645 \u0645\u0633\u0628\u0642\u0627\u064b") { Season = seasonH, Episode = ep });
                    cntExists++;
                }
                else
                {
                    targetSeen[targetPath] = file.Name;
                    L($"\U0001f504 \u0645\u0646: {file.Name} \u25c4 \u0625\u0644\u0649: {newName}");
                    result.Log.Add(new ReportItem("\u0645\u0639\u062f\u0644", null, file.Name, newName, null) { Season = seasonH, Episode = ep });
                    cntMap++;

                    if (!previewOnly)
                    {
                        string oldPath = file.FullName;
                        try
                        {
                            File.Move(oldPath, targetPath);
                            _undoLog.Add(new RenameEntry(oldPath, targetPath));

                            if (renameSubtitles)
                            {
                                RenameMatchingSubtitles(file, newName, result, seasonH, cancellationToken);
                            }
                        }
                        catch
                        {
                            L($"\u274c \u0641\u0634\u0644\u062a \u0625\u0639\u0627\u062f\u0629 \u0627\u0644\u062a\u0633\u0645\u064a\u0629: {file.Name}");
                            result.Log.Add(new ReportItem("\u0645\u062a\u062c\u0627\u0647\u0644", file.Name, null, null, "\u0641\u0634\u0644\u062a \u0639\u0645\u0644\u064a\u0629 \u0625\u0639\u0627\u062f\u0629 \u0627\u0644\u062a\u0633\u0645\u064a\u0629") { Season = seasonH, Episode = ep });
                        }
                    }
                }
            }
            else
            {
                result.Log.Add(new ReportItem("\u0645\u062a\u062c\u0627\u0647\u0644", file.Name, null, null, "\u0644\u0645 \u064a\u0639\u062b\u0631 \u0639\u0644\u0649 \u0631\u0642\u0645 \u062d\u0644\u0642\u0629") { Season = seasonH, Episode = ep });
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
                L($"\u26aa {ig.Name} \u25c4 (\u0627\u0644\u0633\u0628\u0628: {ig.Reason})");
        }
        else
        {
            L("\u26aa \u0644\u0627 \u062a\u0648\u062c\u062f \u0645\u0644\u0641\u0627\u062a \u0645\u062a\u062c\u0627\u0647\u0644\u0629");
        }

        sw.Stop();
        string seconds = (sw.Elapsed.TotalSeconds).ToString("0.0", CultureInfo.InvariantCulture);

        if (previewOnly)
        {
            L("");
            L("\ud83d\udcca \u0645\u0644\u062e\u0635 \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629 (\u0644\u0645 \u064a\u062a\u0645 \u062a\u0646\u0641\u064a\u0630 \u0623\u064a \u062a\u063a\u064a\u064a\u0631):");
            L("--------------------------------------------");
            L($"\u2728 \u0633\u064a\u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0627\u0644\u0627\u0633\u0645 \u0644\u0639\u062f\u062f: {cntMap} \u0645\u0644\u0641");
            if (cntCorrect > 0)
                L($"\u2705 \u0623\u0633\u0645\u0627\u0621 \u0635\u062d\u064a\u062d\u0629 \u0628\u0627\u0644\u0641\u0639\u0644 \u0648\u0644\u0646 \u062a\u0644\u0645\u0633: {cntCorrect} \u0645\u0644\u0641");
            if (ignoredItems.Count > 0)
                L($"\u26aa \u0633\u064a\u062a\u0645 \u062a\u062c\u0627\u0647\u0644\u0647\u0627 \u0648\u062a\u062e\u0637\u064a\u0647\u0627: {ignoredItems.Count} \u0645\u0644\u0641\u0627\u062a");
            L($"\u23f1\ufe0f \u0645\u062f\u0629 \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629: {seconds} \u062b\u0627\u0646\u064a\u0629");
        }
        else
        {
            L("");
            L("\ud83d\udcca \u0645\u0644\u062e\u0635 \u0627\u0644\u0639\u0645\u0644\u064a\u0629:");
            L("--------------------------------------------");
            L($"\u2728 \u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0627\u0644\u0627\u0633\u0645 \u0628\u0646\u062c\u0627\u062d: {cntMap} \u0645\u0644\u0641\u0627\u064b");
            if (cntCorrect > 0)
                L($"\u2705 \u0623\u0633\u0645\u0627\u0621 \u0635\u062d\u064a\u062d\u0629 \u0628\u0627\u0644\u0641\u0639\u0644 (\u0644\u0645 \u062a\u063a\u064a\u0651\u0631): {cntCorrect} \u0645\u0644\u0641\u0627\u064b");
            L($"\u26aa \u062a\u0645 \u062a\u062c\u0627\u0647\u0644\u0647\u0627 \u0648\u062a\u062e\u0637\u064a\u0647\u0627: {ignoredItems.Count} \u0645\u0644\u0641\u0627\u062a");
            L($"\u23f1\ufe0f \u0627\u0644\u0648\u0642\u062a \u0627\u0644\u0645\u0633\u062a\u063a\u0631\u0642: {seconds} \u062b\u0627\u0646\u064a\u0629");
        }

        List<(int Season, int Start, int End)> runsBySeason = new();
        int missingTotal = 0;
        foreach (var seasonPair in seasonEpisodes.OrderBy(pair => pair.Key))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                result.Cancelled = true;
                break;
            }
            MissingAnalysis seasonAnalysis = GapAnalyzer.Analyze(seasonPair.Value, cancellationToken);
            missingTotal += seasonAnalysis.Count;
            foreach (var run in seasonAnalysis.Runs)
                runsBySeason.Add((seasonPair.Key, run.Start, run.End));
        }

        if (!result.Cancelled)
        {
            foreach (var run in runsBySeason)
                result.MissingRuns.Add((run.Start, run.End));
            result.MissingRunsBySeason.AddRange(runsBySeason);

            if (missingTotal > 0)
            {
                L("");
                L($"\u26a0\ufe0f \u062a\u0646\u0628\u064a\u0647 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629 ({missingTotal} \u062d\u0644\u0642\u0629 \u0645\u0641\u0642\u0648\u062f\u0629):");
                L("--------------------------------------------");
                L("\u064a\u0648\u062c\u062f \u0646\u0642\u0635 \u0641\u064a \u062a\u0633\u0644\u0633\u0644 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u062f\u0627\u062e\u0644 \u0627\u0644\u0645\u062c\u0644\u062f\u060c \u0644\u0645 \u064a\u062a\u0645 \u0627\u0644\u0639\u062b\u0648\u0631 \u0639\u0644\u0649:");
                int shown = 0;
                foreach (var run in runsBySeason)
                {
                    if (shown >= 15)
                    {
                        L($"... ({runsBySeason.Count - shown} \u0646\u0637\u0627\u0642\u0627\u062a \u0623\u062e\u0631\u0649)");
                        break;
                    }
                    string prefix = SeasonDisplay(run.Season);
                    if (run.Start == run.End)
                        L($"\u2022 {prefix} \u0627\u0644\u062d\u0644\u0642\u0629 {run.Start}");
                    else
                        L($"\u2022 {prefix} \u0645\u0646 \u0627\u0644\u062d\u0644\u0642\u0629 {run.Start} \u0625\u0644\u0649 {run.End}");
                    shown++;
                }
            }
            else
            {
                L("");
                L("\u2705 \u0644\u0627 \u062a\u0648\u062c\u062f \u062d\u0644\u0642\u0627\u062a \u0645\u0641\u0642\u0648\u062f\u0629");
            }

            string? savedFile = MissingEpisodesWriter.WriteFile(realPath, runsBySeason);
            if (savedFile is not null)
            {
                L("");
                L("\U0001f4be \u062a\u0645 \u062d\u0641\u0638 \u0645\u0644\u0641 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629:");
                L(savedFile);
            }
            else if (runsBySeason.Count > 0)
            {
                string targetFile = Path.Combine(realPath, "\u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629.txt");
                L("");
                L("\u26a0\ufe0f \u062a\u0639\u0630\u0631 \u062d\u0641\u0638 \u0645\u0644\u0641 \u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629: " + targetFile);
            }
            result.MissingFile = savedFile;
        }

        result.Stats = new RunStats(cntMap, cntCorrect, ignoredItems.Count, missingTotal);
        return result;
    }

    private static string SeasonDisplay(int season)
        => season <= 0
            ? "\u063a\u064a\u0631 \u0645\u062d\u062f\u062f"
            : $"\u0627\u0644\u0645\u0648\u0633\u0645 {season.ToString().PadLeft(2, '0')}";

    private void RenameMatchingSubtitles(
        FileInfo videoFile, string newVideoName, RunResult result, int? season, CancellationToken cancellationToken)
    {
        DirectoryInfo? dir = videoFile.Directory;
        if (dir is null) return;

        string videoBase = Path.GetFileNameWithoutExtension(videoFile.Name);
        string newVideoBase = Path.GetFileNameWithoutExtension(newVideoName);

        foreach (FileInfo f in dir.EnumerateFiles("*"))
        {
            if (cancellationToken.IsCancellationRequested) return;
            if (SubtitleExtensions.Contains(f.Extension.TrimStart('.').ToLowerInvariant()))
            {
                string subBase = Path.GetFileNameWithoutExtension(f.Name);
                bool matches = string.Equals(subBase, videoBase, StringComparison.OrdinalIgnoreCase)
                    || subBase.StartsWith(videoBase + ".", StringComparison.OrdinalIgnoreCase);
                if (!matches) continue;

                string suffix = subBase.Length > videoBase.Length ? subBase[videoBase.Length..] : "";
                string newSubName = newVideoBase + suffix + f.Extension;
                string newSubPath = Path.Combine(dir.FullName, newSubName);

                if (string.Equals(newSubPath, f.FullName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (IsExists(newSubPath)) continue;

                try
                {
                    File.Move(f.FullName, newSubPath);
                    _undoLog.Add(new RenameEntry(f.FullName, newSubPath));
                    result.Log.Add(new ReportItem("\u0645\u0639\u062f\u0644", null, f.Name, newSubName, null) { Season = season });
                }
                catch
                {
                }
            }
        }
    }

    private static int? GetSeasonNumber(FileInfo file, string rootPath)
    {
        return EpisodeNameGenerator.GetSeasonNumber(file, rootPath);
    }

    private static bool IsExists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    private static HashSet<string> ParseIgnorePatterns(string? patterns)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(patterns)) return result;
        foreach (string part in patterns.Split(',', '|'))
        {
            string trimmed = part.Trim();
            if (trimmed.Length > 0)
                result.Add(trimmed);
        }
        return result;
    }

    private static bool ShouldIgnore(string fileName, HashSet<string> patterns)
    {
        if (patterns.Count == 0) return false;
        foreach (string pattern in patterns)
        {
            if (fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}