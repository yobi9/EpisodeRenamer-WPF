using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace EpisodeRenamer.App.UI;

internal static class DialogService
{
    internal static bool Confirm(Window? owner, string message, string title)
    {
        if (owner == null) return false;
        if (IsHeadless()) return true;
        var result = MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    internal static string? PickSavePath(Window? owner, string? initialDirectory, string defaultFileName, string filter)
    {
        if (owner == null) return null;
        if (IsHeadless()) return Path.Combine(Path.GetTempPath(), defaultFileName);
        var dlg = new SaveFileDialog
        {
            Filter = filter,
            FileName = defaultFileName
        };
        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            dlg.InitialDirectory = initialDirectory;
        }
        bool? ok = dlg.ShowDialog(owner);
        return ok == true ? dlg.FileName : null;
    }

    private static bool IsHeadless() =>
        Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") == "1";
}