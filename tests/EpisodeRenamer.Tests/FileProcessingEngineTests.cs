using System.IO;
using EpisodeRenamer.Core;
using Xunit;

namespace EpisodeRenamer.Tests;

public class FileProcessingEngineTests : IDisposable
{
    private readonly string _dir;
    private readonly UndoLogManager _undo;
    private readonly FileProcessingEngine _engine;
    private readonly List<(int Current, int Total)> _progress = new();

    public FileProcessingEngineTests()
    {
        _dir = TestHelpers.NewTempDir();
        _undo = new UndoLogManager(Path.Combine(_dir, "undo.json"));
        _engine = new FileProcessingEngine(_undo);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch
        {
        }
    }

    private RunResult Run(string path, bool preview, string? show = null)
    {
        return _engine.Run(
            path,
            previewOnly: preview,
            recurse: false,
            style: "S01E01 (\u0646\u0645\u0637 \u0628\u0644\u064a\u0643\u0633 \u0627\u0644\u0642\u064a\u0627\u0633\u064a)",
            showName: show,
            cleanTags: false,
            progress: (c, t) => _progress.Add((c, t)));
    }

    [Fact]
    public void Preview_InvalidPath_ReturnsErrorLineOnly()
    {
        RunResult r = Run(Path.Combine(_dir, "missing"), preview: true);

        Assert.Equal(ReportExporter.ModePreview, r.Mode);
        Assert.Contains(r.OutputLines, l => l.Contains("\u0627\u0644\u0645\u0633\u0627\u0631 \u063a\u064a\u0631 \u0635\u0627\u0644\u062d"));
    }

    [Fact]
    public void Preview_PlansRenamesAndWritesMissingFile()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.mkv");
        TestHelpers.NewTempFileName(_dir, "ep.S01E02.mkv");
        TestHelpers.NewTempFileName(_dir, "ep.S01E05.mkv");
        TestHelpers.NewTempFileName(_dir, "ep.S01E08.mkv");

        RunResult r = Run(_dir, preview: true, show: "My Show");

        Assert.Equal(4, r.Stats.Mapped);
        Assert.Equal(4, r.Stats.Missing);
        Assert.Equal(2, r.MissingRuns.Count);
        Assert.Equal(3, r.MissingRuns[0].Start);
        Assert.Equal(4, r.MissingRuns[0].End);
        Assert.Equal(6, r.MissingRuns[1].Start);
        Assert.Equal(7, r.MissingRuns[1].End);

        Assert.Contains(r.OutputLines, l => l.Contains("\u0627\u0644\u0645\u0648\u0633\u0645 01"));
        Assert.Contains(r.OutputLines, l => l.Contains("\u0633\u064a\u062a\u0645 \u062a\u0639\u062f\u064a\u0644 \u0627\u0644\u0627\u0633\u0645 \u0644\u0639\u062f\u062f: 4"));

        string missingFile = Path.Combine(_dir, "\u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629.txt");
        Assert.True(File.Exists(missingFile));
        Assert.Equal(missingFile, r.MissingFile);
        Assert.Contains("\u0627\u0644\u062d\u0644\u0642\u0629 3", File.ReadAllText(missingFile));

        Assert.Equal(4, _progress.Count);
        Assert.Contains(_progress, p => p.Total == 4 && p.Current == 4);

        Assert.True(File.Exists(Path.Combine(_dir, "ep.S01E01.mkv")));
        Assert.False(File.Exists(Path.Combine(_dir, "My Show-S01E01.mkv")));
    }

    [Fact]
    public void Execute_RenamesFilesAndLogsUndo()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.mkv");
        TestHelpers.NewTempFileName(_dir, "ep.S01E02.mkv");

        RunResult r = Run(_dir, preview: false, show: "My Show");

        Assert.Equal(ReportExporter.ModeExecute, r.Mode);
        Assert.Equal(2, r.Stats.Mapped);
        Assert.True(File.Exists(Path.Combine(_dir, "My Show-S01E001.mkv")));
        Assert.True(File.Exists(Path.Combine(_dir, "My Show-S01E002.mkv")));
        Assert.False(File.Exists(Path.Combine(_dir, "ep.S01E01.mkv")));

        Assert.Equal(2, _undo.Log.Count);
        IReadOnlyList<(RenameEntry Entry, bool Success)> undoResults = _undo.UndoAll();
        Assert.Equal(2, undoResults.Count);
        Assert.All(undoResults, u => Assert.True(u.Success));
        Assert.True(File.Exists(Path.Combine(_dir, "ep.S01E01.mkv")));
        Assert.True(File.Exists(Path.Combine(_dir, "ep.S01E02.mkv")));
        Assert.Empty(_undo.Log);
    }

    [Fact]
    public void Preview_CorrectName_NotMapped()
    {
        TestHelpers.NewTempFileName(_dir, "My Show-S01E001.mkv");

        RunResult r = Run(_dir, preview: true, show: "My Show");

        Assert.Equal(1, r.Stats.Correct);
        Assert.Equal(0, r.Stats.Mapped);
        Assert.True(File.Exists(Path.Combine(_dir, "My Show-S01E001.mkv")));
    }

    [Fact]
    public void Collision_TwoFilesSameTarget_SecondIgnored()
    {
        TestHelpers.NewTempFileName(_dir, "a.S01E01.mkv");
        TestHelpers.NewTempFileName(_dir, "b.S01E01.mkv");

        RunResult r = Run(_dir, preview: true, show: "My Show");

        Assert.Equal(1, r.Stats.Mapped);
        Assert.True(r.Stats.Ignored > 0);
        Assert.Contains(r.OutputLines, l => l.Contains("\u064a\u0648\u062c\u062f \u0645\u0644\u0641 \u0628\u0646\u0641\u0633 \u0627\u0644\u0627\u0633\u0645 \u0645\u0633\u0628\u0642\u0627\u064b"));
    }

    [Fact]
    public void Collision_TargetAlreadyExistsOnDisk_Ignored()
    {
        TestHelpers.NewTempFileName(_dir, "My Show-S01E001.mkv");
        TestHelpers.NewTempFileName(_dir, "a.S01E01.mkv");

        RunResult r = Run(_dir, preview: true, show: "My Show");

        Assert.Equal(1, r.Stats.Correct);
        Assert.Equal(0, r.Stats.Mapped);
        Assert.True(r.Stats.Ignored > 0);
    }

    [Fact]
    public void NonVideoFile_IgnoredAsNotVideo()
    {
        TestHelpers.NewTempFileName(_dir, "subtitle.srt");

        RunResult r = Run(_dir, preview: true, show: "My Show");

        Assert.True(r.Stats.Ignored > 0);
        Assert.Contains(r.OutputLines, l => l.Contains("\u0644\u064a\u0633 \u0645\u0644\u0641 \u0641\u064a\u062f\u064a\u0648"));
    }

    [Fact]
    public void NoEpisodeNumber_Ignored()
    {
        TestHelpers.NewTempFileName(_dir, "randomfile.mkv");

        RunResult r = Run(_dir, preview: true, show: "My Show");

        Assert.Contains(r.OutputLines, l => l.Contains("\u0644\u0645 \u064a\u0639\u062b\u0631 \u0639\u0644\u0649 \u0631\u0642\u0645 \u062d\u0644\u0642\u0629"));
    }

    [Fact]
    public void AllCorrect_RemovesExistingMissingFile()
    {
        string missingFile = Path.Combine(_dir, "\u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629.txt");
        File.WriteAllText(missingFile, "old");
        TestHelpers.NewTempFileName(_dir, "My Show-S01E001.mkv");

        RunResult r = Run(_dir, preview: true, show: "My Show");

        Assert.Equal(0, r.Stats.Missing);
        Assert.Null(r.MissingFile);
        Assert.False(File.Exists(missingFile));
        Assert.Contains(r.OutputLines, l => l.Contains("\u0644\u0627 \u062a\u0648\u062c\u062f \u062d\u0644\u0642\u0627\u062a \u0645\u0641\u0642\u0648\u062f\u0629"));
    }

    private RunResult RunFull(
        string path, bool preview, string? show = null,
        string? customPattern = null, string? ignorePatterns = null,
        bool renameSubtitles = false, CancellationToken ct = default,
        IReadOnlyDictionary<string, string>? overrides = null)
    {
        return _engine.Run(
            path,
            previewOnly: preview,
            recurse: false,
            style: "S01E01 (\u0646\u0645\u0637 \u0628\u0644\u064a\u0643\u0633 \u0627\u0644\u0642\u064a\u0627\u0633\u064a)",
            showName: show,
            cleanTags: false,
            progress: (c, t) => _progress.Add((c, t)),
            customPattern: customPattern,
            ignorePatterns: ignorePatterns,
            renameSubtitles: renameSubtitles,
            cancellationToken: ct,
            nameOverrides: overrides);
    }

    [Fact]
    public void CustomPattern_EmbedsTokens()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E03.mkv");

        RunResult r = RunFull(_dir, preview: true, show: "My Show",
            customPattern: "{show} - S{season}E{ep3} [My EP{ep2} of {ep}]");

        Assert.Equal(1, r.Stats.Mapped);
        ReportItem item = Assert.Single(r.Log, i => i.Type == "\u0645\u0639\u062f\u0644");
        Assert.Equal("My Show - S1E003 [My EP03 of 3].mkv", item.New);
    }

    [Fact]
    public void CustomPattern_NoShowToken_OmitsShowPrefix()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.mkv");

        RunResult r = RunFull(_dir, preview: true, show: "My Show",
            customPattern: "E{ep2}");

        Assert.Equal(1, r.Stats.Mapped);
        ReportItem item = Assert.Single(r.Log, i => i.Type == "\u0645\u0639\u062f\u0644");
        Assert.Equal("E01.mkv", item.New);
    }

    [Fact]
    public void IgnorePatterns_ExcludesSampleFiles()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.mkv");
        TestHelpers.NewTempFileName(_dir, "show.sample.mkv");

        RunResult r = RunFull(_dir, preview: true, show: "My Show", ignorePatterns: "sample");

        Assert.Equal(1, r.Stats.Mapped);
        Assert.Contains(r.OutputLines, l => l.Contains("\u064a\u0637\u0627\u0628\u0642 \u0646\u0645\u0637 \u0627\u0644\u0627\u0633\u062a\u0628\u0639\u0627\u062f"));
        ReportItem ignored = Assert.Single(r.Log, i => i.Type == "\u0645\u062a\u062c\u0627\u0647\u0644" && i.Name == "show.sample.mkv");
        Assert.Equal("\u064a\u0637\u0627\u0628\u0642 \u0646\u0645\u0637 \u0627\u0644\u0627\u0633\u062a\u0628\u0639\u0627\u062f", ignored.Reason);
    }

    [Fact]
    public void IgnorePatterns_MultiplePatterns_CommaSeparated()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.mkv");
        TestHelpers.NewTempFileName(_dir, "trailer.S01E01.mkv");
        TestHelpers.NewTempFileName(_dir, "extra.S01E01.mkv");

        RunResult r = RunFull(_dir, preview: true, show: "My Show", ignorePatterns: "trailer, extra");

        Assert.Equal(1, r.Stats.Mapped);
        Assert.Equal(2, r.Stats.Ignored);
    }

    [Fact]
    public void Subtitles_RenameMatchingInExecuteMode()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.mkv");
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.srt");
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.en.srt");

        RunFull(_dir, preview: false, show: "My Show", renameSubtitles: true);

        Assert.True(File.Exists(Path.Combine(_dir, "My Show-S01E001.mkv")));
        Assert.True(File.Exists(Path.Combine(_dir, "My Show-S01E001.srt")));
        Assert.True(File.Exists(Path.Combine(_dir, "My Show-S01E001.en.srt")));
        Assert.False(File.Exists(Path.Combine(_dir, "ep.S01E01.srt")));
    }

    [Fact]
    public void Subtitles_NotRenamedWhenFlagOff()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.mkv");
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.srt");

        RunFull(_dir, preview: false, show: "My Show", renameSubtitles: false);

        Assert.True(File.Exists(Path.Combine(_dir, "My Show-S01E001.mkv")));
        Assert.True(File.Exists(Path.Combine(_dir, "ep.S01E01.srt")));
    }

    [Fact]
    public void Cancellation_StopsEarlyAndMarksCancelled()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.mkv");
        TestHelpers.NewTempFileName(_dir, "ep.S01E02.mkv");
        TestHelpers.NewTempFileName(_dir, "ep.S01E03.mkv");

        var cts = new CancellationTokenSource();
        RunResult r = _engine.Run(
            _dir,
            previewOnly: true,
            recurse: false,
            style: "S01E01 (\u0646\u0645\u0637 \u0628\u0644\u064a\u0643\u0633 \u0627\u0644\u0642\u064a\u0627\u0633\u064a)",
            showName: "My Show",
            cleanTags: false,
            progress: (c, t) =>
            {
                _progress.Add((c, t));
                cts.Cancel();
            },
            cancellationToken: cts.Token);

        Assert.True(r.Cancelled);
        Assert.Contains(r.OutputLines, l => l.Contains("\u062a\u0645 \u0625\u064a\u0642\u0627\u0641 \u0627\u0644\u0639\u0645\u0644\u064a\u0629 \u064a\u062f\u0648\u064a\u0627\u064b"));
        Assert.Equal(1, r.Stats.Mapped);
    }

    [Fact]
    public void NameOverride_ReplacesGeneratedName()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E05.mkv");

        RunResult r = RunFull(_dir, preview: true, show: "My Show",
            overrides: new Dictionary<string, string> { ["ep.S01E05.mkv"] = "My Custom-Name.mkv" });

        ReportItem item = Assert.Single(r.Log, i => i.Type == "\u0645\u0639\u062f\u0644");
        Assert.Equal("My Custom-Name.mkv", item.New);
    }

    [Fact]
    public void NameOverride_EqualToOriginal_CountsAsCorrect()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.mkv");

        RunResult r = RunFull(_dir, preview: true, show: "My Show",
            overrides: new Dictionary<string, string> { ["ep.S01E01.mkv"] = "ep.S01E01.mkv" });

        Assert.Equal(1, r.Stats.Correct);
        Assert.Equal(0, r.Stats.Mapped);
    }
}