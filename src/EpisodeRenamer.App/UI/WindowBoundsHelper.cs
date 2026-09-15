using System;
using System.Windows;
using EpisodeRenamer.Core;

namespace EpisodeRenamer.App.UI;

internal static class WindowBoundsHelper
{
    internal static void Apply(Window window, AppSettings? settings)
    {
        if (window is null || settings is null) return;
        if (settings.WindowWidth is not double width) return;
        if (settings.WindowHeight is not double height) return;
        if (settings.WindowLeft is not double left) return;
        if (settings.WindowTop is not double top) return;
        if (width < window.MinWidth || height < window.MinHeight) return;

        var rect = new Rect(left, top, width, height);
        var virtualScreen = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        if (!virtualScreen.IntersectsWith(rect)) return;

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = left;
        window.Top = top;
        window.Width = width;
        window.Height = height;
        if (settings.WindowMaximized == true) window.WindowState = WindowState.Maximized;
    }

    internal static void Capture(Window window, AppSettings settings)
    {
        if (window is null || settings is null) return;
        var restore = window.RestoreBounds;
        settings.WindowLeft = restore.IsEmpty ? window.Left : restore.Left;
        settings.WindowTop = restore.IsEmpty ? window.Top : restore.Top;
        settings.WindowWidth = restore.IsEmpty ? window.Width : restore.Width;
        settings.WindowHeight = restore.IsEmpty ? window.Height : restore.Height;
        settings.WindowMaximized = window.WindowState == WindowState.Maximized;
    }
}