using System;
using EpisodeRenamer.App;
using Xunit;

namespace EpisodeRenamer.Tests.UI;

public class MainWindowTests : IDisposable
{
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
    public void StyleItems_HasEightEntries()
    {
        Assert.Equal(8, MainWindow.StyleItems.Length);
    }

    [Fact]
    public void StyleItems_MatchesEngineStyleZero()
    {
        Assert.Equal("S01E01 (\u0646\u0645\u0637 \u0628\u0644\u064a\u0643\u0633 \u0627\u0644\u0642\u064a\u0627\u0633\u064a)",
                      MainWindow.StyleItems[0]);
    }

    [Fact]
    public void MainWindow_Initializes()
    {
        var ex = Record.Exception(() =>
        {
            RunOnSta(() =>
            {
                var w = new MainWindow();
                try
                {
                    Assert.NotNull(w);
                    Assert.Equal(8, MainWindow.StyleItems.Length);
                }
                finally { w.Close(); }
            });
        });
        Assert.Null(ex);
    }

    [Fact]
    public void IsHeadlessMode_ReturnsFalse_WhenEnvNotSet()
    {
        string saved = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", null);
            Assert.False(MainWindow.IsHeadlessMode);
        }
        finally { Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", saved); }
    }

    [Fact]
    public void IsHeadlessMode_ReturnsTrue_WhenEnvIs1()
    {
        string saved = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", "1");
            Assert.True(MainWindow.IsHeadlessMode);
        }
        finally { Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", saved); }
    }

    [Fact]
    public void HeadlessMode_BrowseButton_AppendsStubMessage()
    {
        string saved = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", "1");
            string? captured = null;
            RunOnSta(() =>
            {
                var w = new MainWindow();
                try
                {
                    w.browseBtn.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    captured = w.OutputText;
                }
                finally { w.Close(); }
            });
            Assert.NotNull(captured);
            Assert.Contains(MainWindow.HeadlessMarker, captured);
            Assert.Contains("browse folder (stubbed)", captured);
        }
        finally { Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", saved); }
    }

    [Fact]
    public void HeadlessMode_PreviewButton_AppendsStubMessage()
    {
        string saved = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", "1");
            string? captured = null;
            RunOnSta(() =>
            {
                var w = new MainWindow();
                try
                {
                    w.previewBtn.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    captured = w.OutputText;
                }
                finally { w.Close(); }
            });
            Assert.Contains("preview (stubbed)", captured!);
        }
        finally { Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", saved); }
    }

    [Fact]
    public void HeadlessMode_RenameButton_AppendsStubMessage()
    {
        string saved = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", "1");
            string? captured = null;
            RunOnSta(() =>
            {
                var w = new MainWindow();
                try
                {
                    w.renameBtn.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    captured = w.OutputText;
                }
                finally { w.Close(); }
            });
            Assert.Contains("rename (stubbed)", captured!);
        }
        finally { Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", saved); }
    }

    [Fact]
    public void HeadlessMode_UndoButton_AppendsStubMessage()
    {
        string saved = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", "1");
            string? captured = null;
            RunOnSta(() =>
            {
                var w = new MainWindow();
                try
                {
                    w.undoBtn.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    captured = w.OutputText;
                }
                finally { w.Close(); }
            });
            Assert.Contains("undo (stubbed)", captured!);
        }
        finally { Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", saved); }
    }

    [Fact]
    public void HeadlessMode_ExportButton_AppendsStubMessage()
    {
        string saved = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", "1");
            string? captured = null;
            RunOnSta(() =>
            {
                var w = new MainWindow();
                try
                {
                    w.exportBtn.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    captured = w.OutputText;
                }
                finally { w.Close(); }
            });
            Assert.Contains("export (stubbed)", captured!);
        }
        finally { Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", saved); }
    }

    [Fact]
    public void HeadlessMode_SuggestButton_AppendsStubMessage()
    {
        string saved = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", "1");
            string? captured = null;
            RunOnSta(() =>
            {
                var w = new MainWindow();
                try
                {
                    w.suggestBtn.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    captured = w.OutputText;
                }
                finally { w.Close(); }
            });
            Assert.Contains("suggest name (stubbed)", captured!);
        }
        finally { Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", saved); }
    }
}