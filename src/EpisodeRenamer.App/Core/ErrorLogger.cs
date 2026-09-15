using System;
using System.IO;

namespace EpisodeRenamer.Core;

public static class ErrorLogger
{
    public static string LogPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EpisodeRenamer", "error.log");

    public static void Write(Exception? exception, string? context = null)
    {
        if (exception is null) return;
        try
        {
            string? dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using var writer = new StreamWriter(LogPath, append: true);
            writer.WriteLine("===== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " =====");
            if (!string.IsNullOrWhiteSpace(context)) writer.WriteLine("السياق: " + context);
            writer.WriteLine("النوع: " + exception.GetType().FullName);
            writer.WriteLine("الرسالة: " + exception.Message);
            writer.WriteLine(exception.StackTrace ?? "(بدون StackTrace)");
            writer.WriteLine();
        }
        catch
        {
        }
    }
}