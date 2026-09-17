using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using EpisodeRenamer.App;
using EpisodeRenamer.App.UI;
using EpisodeRenamer.Core;
using Xunit;

namespace EpisodeRenamer.Tests.UI;

/// <summary>
/// F-01: persistent inline status strip — resolver precedence, state detection,
/// read-only report-warning simulation, and UI application. No Core changes.
/// </summary>
public class RunStatusTests : IDisposable
{
    private readonly string _dir = TestHelpers.NewTempDir();

    public void Dispose()
    {
        try
        {
            RestoreWrite(_dir);
            Directory.Delete(_dir, recursive: true);
        }
        catch
        {
        }
        GC.SuppressFinalize(this);
    }

    private static void RunOnSta(Action action)
    {
        Exception? error = null;
        var thread = new System.Threading.Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(10))) throw new TimeoutException("STA thread timed out");
        if (error != null) throw error;
    }

    private static void DenyWrite(string dir)
    {
        var info = new DirectoryInfo(dir);
        var security = info.GetAccessControl();
        string user = WindowsIdentity.GetCurrent().Name;
        security.AddAccessRule(new FileSystemAccessRule(
            user,
            FileSystemRights.Write | FileSystemRights.CreateFiles,
            AccessControlType.Deny));
        info.SetAccessControl(security);
    }

    private static void RestoreWrite(string dir)
    {
        try
        {
            var info = new DirectoryInfo(dir);
            var security = info.GetAccessControl();
            string user = WindowsIdentity.GetCurrent().Name;
            security.RemoveAccessRule(new FileSystemAccessRule(
                user,
                FileSystemRights.Write | FileSystemRights.CreateFiles,
                AccessControlType.Deny));
            info.SetAccessControl(security);
        }
        catch
        {
        }
    }

    private static RunResult EnginePreview(string path)
    {
        var undo = new UndoLogManager(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var engine = new FileProcessingEngine(undo);
        return engine.Run(
            path,
            previewOnly: true,
            recurse: false,
            style: "S01E01 (\u0646\u0645\u0637 \u0628\u0644\u064a\u0643\u0633 \u0627\u0644\u0642\u064a\u0627\u0633\u064a)",
            showName: "S",
            cleanTags: false,
            progress: (_, _) => { });
    }

    [Fact]
    public void Resolve_InvalidPath_ReturnsInvalidWithAttemptedPathAsDetail()
    {
        string bad = Path.Combine(_dir, "missing");

        RunStatus s = RunStatusResolver.Resolve(new RunResult(), bad);

        Assert.Equal(RunStatusKind.Invalid, s.Kind);
        Assert.Contains("\u0627\u0644\u0645\u0633\u0627\u0631 \u063a\u064a\u0631 \u0635\u0627\u0644\u062d", s.ShortText);
        Assert.Equal(bad, s.Detail);
    }

    [Fact]
    public void Resolve_ValidFolderWithoutVideos_ReturnsEmptyDistinctFromInvalid()
    {
        RunStatus s = RunStatusResolver.Resolve(new RunResult(), _dir);

        Assert.Equal(RunStatusKind.Empty, s.Kind);
        Assert.NotEqual(RunStatusKind.Invalid, s.Kind);
        Assert.Contains("\u0644\u0627 \u062a\u0648\u062c\u062f \u0645\u0644\u0641\u0627\u062a \u0641\u064a\u062f\u064a\u0648", s.ShortText);
    }

    [Fact]
    public void Resolve_SuccessResult_ContainsCountsAndNoWarning()
    {
        var r = new RunResult { Mode = ReportExporter.ModePreview, Stats = new RunStats(2, 1, 1, 0) };
        r.Log.Add(new ReportItem("\u0645\u0639\u062f\u0644", null, "a.mkv", "b.mkv", null));
        r.MissingFile = Path.Combine(_dir, "saved.txt");

        RunStatus s = RunStatusResolver.Resolve(r, _dir);

        Assert.Equal(RunStatusKind.Success, s.Kind);
        Assert.Contains("2", s.ShortText);
        Assert.Contains("1", s.ShortText);
        Assert.Null(s.Detail);
    }

    [Fact]
    public void Preview_ReadOnlyFolderWithGaps_WarnsButKeepsResults()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.mkv");
        TestHelpers.NewTempFileName(_dir, "ep.S01E02.mkv");
        TestHelpers.NewTempFileName(_dir, "ep.S01E05.mkv");
        DenyWrite(_dir);
        try
        {
            RunResult r = EnginePreview(_dir);

            // A+ Core contract: warning recorded, nothing saved.
            Assert.Null(r.MissingFile);
            Assert.NotEmpty(r.MissingRunsBySeason);
            Assert.Contains(r.OutputLines, l => l.Contains("\u062a\u0639\u0630\u0631 \u062d\u0641\u0638"));

            // Preview itself remains successful and populated.
            Assert.True(r.Stats.Mapped > 0);
            Assert.NotEmpty(r.Log);

            // F-01 surface: visible warning, full target path in the tooltip detail.
            RunStatus s = RunStatusResolver.Resolve(r, _dir);
            Assert.Equal(RunStatusKind.ReportWarning, s.Kind);
            Assert.Contains("\u0627\u0643\u062a\u0645\u0644\u062a \u0627\u0644\u0645\u0639\u0627\u064a\u0646\u0629 \u0628\u0646\u062c\u0627\u062d", s.ShortText);
            Assert.NotNull(s.Detail);
            Assert.Contains("\u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629.txt", s.Detail);
        }
        finally
        {
            RestoreWrite(_dir);
        }
    }

    [Fact]
    public void Preview_ReadOnlyFolderWithoutGaps_StaysSuccessWithNoFalseWarning()
    {
        TestHelpers.NewTempFileName(_dir, "ep.S01E01.mkv");
        TestHelpers.NewTempFileName(_dir, "ep.S01E02.mkv");
        DenyWrite(_dir);
        try
        {
            RunResult r = EnginePreview(_dir);

            Assert.Empty(r.MissingRunsBySeason);

            RunStatus s = RunStatusResolver.Resolve(r, _dir);
            Assert.Equal(RunStatusKind.Success, s.Kind);
        }
        finally
        {
            RestoreWrite(_dir);
        }
    }

    [Fact]
    public void Resolve_Precedence_InvalidBeatsWarningBeatsEmptyBeatsSuccess()
    {
        var warningShaped = new RunResult { Mode = ReportExporter.ModePreview };
        warningShaped.Log.Add(new ReportItem("\u0645\u0639\u062f\u0644", null, "a.mkv", "b.mkv", null));
        warningShaped.MissingRunsBySeason.Add((1, 3, 4));

        // Invalid wins even when the result also carries warning signals.
        Assert.Equal(RunStatusKind.Invalid,
            RunStatusResolver.Resolve(warningShaped, Path.Combine(_dir, "missing")).Kind);

        // Warning wins over Empty (no rows) when gaps exist without a saved file.
        var warningNoRows = new RunResult { Mode = ReportExporter.ModePreview };
        warningNoRows.MissingRunsBySeason.Add((1, 3, 4));
        Assert.Equal(RunStatusKind.ReportWarning,
            RunStatusResolver.Resolve(warningNoRows, _dir).Kind);

        // Empty wins over Success when the directory exists but produced no rows.
        Assert.Equal(RunStatusKind.Empty,
            RunStatusResolver.Resolve(new RunResult(), _dir).Kind);

        // Saved report file suppresses the warning.
        var saved = new RunResult { Mode = ReportExporter.ModePreview };
        saved.Log.Add(new ReportItem("\u0645\u0639\u062f\u0644", null, "a.mkv", "b.mkv", null));
        saved.MissingRunsBySeason.Add((1, 3, 4));
        saved.MissingFile = Path.Combine(_dir, "saved.txt");
        Assert.Equal(RunStatusKind.Success,
            RunStatusResolver.Resolve(saved, _dir).Kind);
    }

    [Fact]
    public void MainWindow_InitialStatus_IsIdle()
    {
        RunOnSta(() =>
        {
            string settingsPath = Path.Combine(_dir, "settings.json");
            string undoPath = Path.Combine(_dir, "undo.json");
            var w = new MainWindow(settingsPath, undoPath);
            try
            {
                Assert.Equal(RunStatusKind.Idle, w.CurrentStatus.Kind);
                Assert.Contains("\u0645\u0639\u0627\u064a\u0646\u0629", w.statusText.Text);
            }
            finally { w.Close(); }
        });
    }

    [Fact]
    public void MainWindow_SetStatus_AppliesTextAndTooltip()
    {
        RunOnSta(() =>
        {
            string settingsPath = Path.Combine(_dir, "settings.json");
            string undoPath = Path.Combine(_dir, "undo.json");
            var w = new MainWindow(settingsPath, undoPath);
            try
            {
                var warning = new RunStatus(RunStatusKind.ReportWarning, "short", "full-detail");
                w.SetStatus(warning);

                Assert.Equal(RunStatusKind.ReportWarning, w.CurrentStatus.Kind);
                Assert.Equal("short", w.statusText.Text);
                Assert.Equal("full-detail", w.statusText.ToolTip);
                Assert.NotNull(w.statusBorder.Background);
            }
            finally { w.Close(); }
        });
    }
}
