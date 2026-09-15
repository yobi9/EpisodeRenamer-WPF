using System.IO;
using System.Text;
using EpisodeRenamer.Core;

namespace EpisodeRenamer.Tests;

public class MissingEpisodesWriterTests
{
    [Fact]
    public void WriteFile_WithMissingEpisodes_CreatesFileWithBom()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "ERTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tmp);
            var analysis = GapAnalyzer.Analyze(new[] { 1, 2, 4 });
            string? result = MissingEpisodesWriter.WriteFile(tmp, analysis);

            Assert.NotNull(result);
            Assert.True(File.Exists(result));

            byte[] bom = File.ReadAllBytes(result)[..3];
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bom);

            string content = File.ReadAllText(result, Encoding.UTF8);
            Assert.Contains("\u0627\u0644\u062d\u0644\u0642\u0629 3 \u0645\u0641\u0642\u0648\u062f\u0629", content);
            Assert.Contains("3", content);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void WriteFile_NoMissing_DeletesExistingFile()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "ERTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tmp);
            string targetFile = Path.Combine(tmp, "\u0627\u0644\u062d\u0644\u0642\u0627\u062a \u0627\u0644\u0645\u0641\u0642\u0648\u062f\u0629.txt");
            File.WriteAllText(targetFile, "old", Encoding.UTF8);
            var analysis = GapAnalyzer.Analyze(new[] { 1, 2 });
            string? result = MissingEpisodesWriter.WriteFile(tmp, analysis);
            Assert.Null(result);
            Assert.False(File.Exists(targetFile));
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void WriteFile_NullPath_ReturnsNull()
        => Assert.Null(MissingEpisodesWriter.WriteFile(null, new MissingAnalysis(0, Array.Empty<int>(), Array.Empty<(int, int)>())));

    [Fact]
    public void WriteFile_NonexistentPath_ReturnsNull()
        => Assert.Null(MissingEpisodesWriter.WriteFile("C:\\Does\\Not\\Exist", new MissingAnalysis(1, new[] { 1 }, new[] { (1, 1) })));
}
