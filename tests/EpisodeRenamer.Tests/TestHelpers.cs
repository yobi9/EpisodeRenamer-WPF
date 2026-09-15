using System.IO;

namespace EpisodeRenamer.Tests;

internal static class TestHelpers
{
    private const string Chars = "abcdefghijklmnopqrstuvwxyz";

    public static string NewTempDir()
    {
        var random = new Random();
        var builder = new System.Text.StringBuilder("ER");
        for (int i = 0; i < 12; i++)
            builder.Append(Chars[random.Next(Chars.Length)]);
        string dir = Path.Combine(Path.GetTempPath(), builder.ToString());
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string NewTempFileName(string dir, string name)
    {
        string path = Path.Combine(dir, name);
        File.WriteAllText(path, "test");
        return path;
    }
}