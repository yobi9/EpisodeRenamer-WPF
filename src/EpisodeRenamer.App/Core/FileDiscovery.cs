using System.IO;

namespace EpisodeRenamer.Core;

public sealed record DiscoveryResult(
    IReadOnlyList<FileInfo> All,
    IReadOnlyList<FileInfo> Video,
    IReadOnlyList<FileInfo> Ignored);

public static class FileDiscovery
{
    public static readonly string[] VideoExtensions =
    {
        "3gp", "3g2", "asf", "avi", "divx", "f4v", "flv", "m2ts", "m4v", "mkv",
        "mov", "mp4", "mpe", "mpeg", "mpg", "mts", "ogv", "rm", "rmvb", "ts",
        "vob", "webm", "wmv"
    };

    private static readonly HashSet<string> VideoSet =
        new(VideoExtensions.Select(e => e.ToLowerInvariant()));

    public static bool IsVideoFile(FileSystemInfo file)
    {
        string ext = Path.GetExtension(file.Name).TrimStart('.').ToLowerInvariant();
        return VideoSet.Contains(ext);
    }

    public static DiscoveryResult Discover(string path, bool recurse)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = recurse,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.None
        };

        List<FileInfo> all = Directory.EnumerateFiles(path, "*", options)
            .Select(f => new FileInfo(f))
            .ToList();

        List<FileInfo> video = new();
        List<FileInfo> ignored = new();
        foreach (FileInfo file in all)
        {
            if (IsVideoFile(file)) video.Add(file);
            else ignored.Add(file);
        }

        return new DiscoveryResult(all, video, ignored);
    }
}