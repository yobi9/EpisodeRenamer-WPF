using System;
using System.IO;
using EpisodeRenamer.App;
using EpisodeRenamer.Core;
using Xunit;

namespace EpisodeRenamer.Tests.UI;

public class MainWindowV12Tests : IDisposable
{
    private readonly string _dir = TestHelpers.NewTempDir();

    public void Dispose() { GC.SuppressFinalize(this); }

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

    [Fact]
    public void PromptPendingUndo_RestoresFilesFromPrevSession()
    {
        string oldEnv = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", "1");
            string old1 = TestHelpers.NewTempFileName(_dir, "episode1.mkv");
            string old2 = TestHelpers.NewTempFileName(_dir, "episode2.mkv");
            string new1 = Path.Combine(_dir, "My Show-S01E001.mkv");
            string new2 = Path.Combine(_dir, "My Show-S01E002.mkv");
            File.Move(old1, new1);
            File.Move(old2, new2);

            string undoPath = Path.Combine(_dir, "undo.json");
            string settingsPath = Path.Combine(_dir, "settings.json");
            var undo = new UndoLogManager(undoPath);
            undo.Add(new RenameEntry(old1, new1));
            undo.Add(new RenameEntry(old2, new2));

            RunOnSta(() =>
            {
                var w = new MainWindow(settingsPath, undoPath);
                try
                {
                    w.PromptPendingUndo();

                    Assert.True(File.Exists(old1));
                    Assert.True(File.Exists(old2));
                    Assert.False(File.Exists(new1));
                    Assert.False(File.Exists(new2));
                    Assert.Contains("\u26A0\uFE0F", w.OutputText);
                    Assert.Contains("\u0627\u0643\u062A\u0645\u0644 \u0627\u0644\u062A\u0631\u0627\u062C\u0639", w.OutputText);
                }
                finally { w.Close(); }
            });

            undo.Load();
            Assert.Empty(undo.Log);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", string.IsNullOrEmpty(oldEnv) ? null : oldEnv);
        }
    }

    [Fact]
    public void Constructor_RestoresSavedWindowGeometry()
    {
        string oldEnv = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", null);
            string settingsPath = Path.Combine(_dir, "settings.json");
            var settings = new SettingsManager(settingsPath);
            settings.Save(new AppSettings
            {
                WindowLeft = 40,
                WindowTop = 35,
                WindowWidth = 920,
                WindowHeight = 720,
                WindowMaximized = false
            });

            RunOnSta(() =>
            {
                var w = new MainWindow(settingsPath, Path.Combine(_dir, "undo.json"));
                try
                {
                    Assert.Equal(40, w.Left);
                    Assert.Equal(35, w.Top);
                    Assert.Equal(920, w.Width);
                    Assert.Equal(720, w.Height);
                    Assert.Equal(System.Windows.WindowState.Normal, w.WindowState);
                }
                finally { w.Close(); }
            });
        }
        finally
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", string.IsNullOrEmpty(oldEnv) ? null : oldEnv);
        }
    }

    [Fact]
    public void WindowClosed_SavesWindowGeometry()
    {
        string oldEnv = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", null);
            string settingsPath = Path.Combine(_dir, "settings.json");

            RunOnSta(() =>
            {
                var w = new MainWindow(settingsPath, Path.Combine(_dir, "undo.json"));
                w.Left = 55;
                w.Top = 45;
                w.Width = 920;
                w.Height = 720;
                w.Close();
            });

            AppSettings? saved = new SettingsManager(settingsPath).Load();
            Assert.NotNull(saved);
            Assert.Equal(55, saved!.WindowLeft);
            Assert.Equal(45, saved.WindowTop);
            Assert.Equal(920, saved.WindowWidth);
            Assert.Equal(720, saved.WindowHeight);
            Assert.False(saved.WindowMaximized);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", string.IsNullOrEmpty(oldEnv) ? null : oldEnv);
        }
    }
}