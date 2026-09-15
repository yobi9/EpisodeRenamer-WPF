using System;
using System.IO;
using EpisodeRenamer.Core;
using Xunit;

namespace EpisodeRenamer.Tests;

public class ErrorLoggerTests : IDisposable
{
    private readonly string _dir = TestHelpers.NewTempDir();
    private readonly string _originalLogPath;

    public ErrorLoggerTests()
    {
        _originalLogPath = ErrorLogger.LogPath;
        ErrorLogger.LogPath = Path.Combine(_dir, "error.log");
    }

    public void Dispose()
    {
        ErrorLogger.LogPath = _originalLogPath;
    }

    [Fact]
    public void Write_CreatesLogFile_WithExceptionDetails()
    {
        ErrorLogger.Write(new InvalidOperationException("boom"), "test-context");

        Assert.True(File.Exists(ErrorLogger.LogPath));
        string content = File.ReadAllText(ErrorLogger.LogPath);
        Assert.Contains("InvalidOperationException", content);
        Assert.Contains("boom", content);
        Assert.Contains("test-context", content);
        Assert.Contains("السياق", content);
        Assert.Contains("الرسالة", content);
    }

    [Fact]
    public void Write_AppendsMultipleEntries()
    {
        ErrorLogger.Write(new Exception("first"));
        ErrorLogger.Write(new Exception("second"));

        string content = File.ReadAllText(ErrorLogger.LogPath);
        Assert.Equal(2, content.Split('\n').Count(l => l.StartsWith("=====", StringComparison.Ordinal)));
    }

    [Fact]
    public void Write_Null_DoesNothing()
    {
        ErrorLogger.Write(null);

        Assert.False(File.Exists(ErrorLogger.LogPath));
    }

    [Fact]
    public void DefaultLogPath_PointsToAppDataEpisodeRenamer()
    {
        string original = ErrorLogger.LogPath;
        try
        {
            ErrorLogger.LogPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EpisodeRenamer", "error.log");

            Assert.EndsWith("EpisodeRenamer" + Path.DirectorySeparatorChar + "error.log", ErrorLogger.LogPath);
        }
        finally
        {
            ErrorLogger.LogPath = original;
        }
    }
}