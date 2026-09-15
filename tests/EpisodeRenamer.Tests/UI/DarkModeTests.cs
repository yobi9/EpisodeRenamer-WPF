using System;
using System.IO;
using System.Windows.Media;
using EpisodeRenamer.App;
using EpisodeRenamer.App.UI;
using EpisodeRenamer.Core;
using Xunit;

namespace EpisodeRenamer.Tests.UI;

public class DarkModeTests : IDisposable
{
    private readonly string _dir = TestHelpers.NewTempDir();
    private readonly string _savedHeadless;

    public DarkModeTests()
    {
        _savedHeadless = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS",
            string.IsNullOrEmpty(_savedHeadless) ? null : _savedHeadless);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ApplyTheme_SetsDarkPalette()
    {
        RunOnSta(() =>
        {
            var w = new MainWindow(SettingsPath(), UndoPath());
            try
            {
                w.ApplyTheme(true);

                Assert.True(IsDarkWindow(w));
                Assert.Equal(ThemeManager.DarkGlyph, w.themeBtn.Content);
            }
            finally { w.Close(); }
        });
    }

    [Fact]
    public void ApplyTheme_SetsLightPalette()
    {
        RunOnSta(() =>
        {
            var w = new MainWindow(SettingsPath(), UndoPath());
            try
            {
                w.ApplyTheme(true);
                w.ApplyTheme(false);

                Assert.False(IsDarkWindow(w));
                Assert.Equal(ThemeManager.LightGlyph, w.themeBtn.Content);
            }
            finally { w.Close(); }
        });
    }

    [Fact]
    public void Constructor_AppliesSavedDarkTheme()
    {
        RunOnSta(() =>
        {
            var manager = new SettingsManager(SettingsPath());
            manager.Save(new AppSettings { DarkTheme = true });

            var w = new MainWindow(SettingsPath(), UndoPath());
            try
            {
                Assert.True(IsDarkWindow(w));
                Assert.Equal(ThemeManager.DarkGlyph, w.themeBtn.Content);
            }
            finally { w.Close(); }
        });
    }

    [Fact]
    public void WindowClosed_SavesDarkTheme()
    {
        string oldEnv = Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", null);
            string settingsPath = SettingsPath();

            RunOnSta(() =>
            {
                var w = new MainWindow(settingsPath, UndoPath());
                w.ApplyTheme(true);
                w.Close();
            });

            bool exists = File.Exists(settingsPath);
            string diag = "";
            if (!exists)
            {
                bool closedFired = false;
                RunOnSta(() =>
                {
                    var probe = new MainWindow(settingsPath, UndoPath());
                    probe.ApplyTheme(true);
                    probe.Closed += (_, _) => closedFired = true;
                    probe.Close();
                });
                new SettingsManager(settingsPath).Save(new AppSettings { Path = "probe" });
                diag = " | direct-save-exists=" + File.Exists(settingsPath) + " | closedFired=" + closedFired;
            }
            Assert.True(exists, "Settings file was not created at: " + settingsPath + " | IsHeadless=" + MainWindow.IsHeadlessMode + diag);
            AppSettings? loaded = new SettingsManager(settingsPath).Load();
            Assert.NotNull(loaded);
            Assert.True(loaded!.DarkTheme);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EPISODE_RENAMER_HEADLESS", string.IsNullOrEmpty(oldEnv) ? null : oldEnv);
        }
    }

    private string SettingsPath() => Path.Combine(_dir, "settings.json");
    private string UndoPath() => Path.Combine(_dir, "undo.json");

    private static bool IsDarkWindow(MainWindow window) =>
        window.Resources[ThemeManager.KeyWindowBackground] is SolidColorBrush brush &&
        brush.Color == Color.FromRgb(0x1E, 0x1E, 0x1E);

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
}