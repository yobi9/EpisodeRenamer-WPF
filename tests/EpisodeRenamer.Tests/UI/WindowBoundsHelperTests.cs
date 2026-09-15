using System;
using System.Windows;
using EpisodeRenamer.App.UI;
using EpisodeRenamer.Core;
using Xunit;

namespace EpisodeRenamer.Tests.UI;

public class WindowBoundsHelperTests
{
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
    public void Apply_AppliesValidGeometry()
    {
        RunOnSta(() =>
        {
            var window = new Window { MinWidth = 200, MinHeight = 100 };
            var settings = new AppSettings
            {
                WindowLeft = 50,
                WindowTop = 30,
                WindowWidth = 800,
                WindowHeight = 500,
                WindowMaximized = false
            };

            WindowBoundsHelper.Apply(window, settings);

            Assert.Equal(50, window.Left);
            Assert.Equal(30, window.Top);
            Assert.Equal(800, window.Width);
            Assert.Equal(500, window.Height);
            Assert.Equal(WindowStartupLocation.Manual, window.WindowStartupLocation);
            Assert.Equal(WindowState.Normal, window.WindowState);
        });
    }

    [Fact]
    public void Apply_AppliesMaximized()
    {
        RunOnSta(() =>
        {
            var window = new Window { MinWidth = 200, MinHeight = 100 };
            var settings = new AppSettings
            {
                WindowLeft = 10,
                WindowTop = 10,
                WindowWidth = 800,
                WindowHeight = 500,
                WindowMaximized = true
            };

            WindowBoundsHelper.Apply(window, settings);

            Assert.Equal(WindowState.Maximized, window.WindowState);
        });
    }

    [Fact]
    public void Apply_IgnoresTooSmall()
    {
        RunOnSta(() =>
        {
            var window = new Window { MinWidth = 200, MinHeight = 100 };
            var settings = new AppSettings
            {
                WindowLeft = 10,
                WindowTop = 10,
                WindowWidth = 120,
                WindowHeight = 80,
                WindowMaximized = false
            };

            WindowBoundsHelper.Apply(window, settings);

            Assert.True(double.IsNaN(window.Left));
            Assert.True(double.IsNaN(window.Width));
        });
    }

    [Fact]
    public void Apply_IgnoresOffScreen()
    {
        RunOnSta(() =>
        {
            var window = new Window { MinWidth = 200, MinHeight = 100 };
            var settings = new AppSettings
            {
                WindowLeft = -50000,
                WindowTop = -50000,
                WindowWidth = 800,
                WindowHeight = 500,
                WindowMaximized = false
            };

            WindowBoundsHelper.Apply(window, settings);

            Assert.True(double.IsNaN(window.Left));
            Assert.True(double.IsNaN(window.Top));
        });
    }

    [Fact]
    public void Apply_NullSettings_DoesNothing()
    {
        RunOnSta(() =>
        {
            var window = new Window();
            var ex = Record.Exception(() => WindowBoundsHelper.Apply(window, null));
            Assert.Null(ex);
        });
    }

    [Fact]
    public void Capture_RecordsCurrentBounds()
    {
        RunOnSta(() =>
        {
            var window = new Window
            {
                Left = 15,
                Top = 25,
                Width = 700,
                Height = 450,
                WindowState = WindowState.Normal
            };
            var settings = new AppSettings();

            WindowBoundsHelper.Capture(window, settings);

            Assert.Equal(15, settings.WindowLeft);
            Assert.Equal(25, settings.WindowTop);
            Assert.Equal(700, settings.WindowWidth);
            Assert.Equal(450, settings.WindowHeight);
            Assert.False(settings.WindowMaximized);
        });
    }
}