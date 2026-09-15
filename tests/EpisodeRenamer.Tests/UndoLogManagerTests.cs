using System.IO;
using System.Text;
using EpisodeRenamer.Core;
using Xunit;

namespace EpisodeRenamer.Tests;

public class UndoLogManagerTests
{
    private readonly string _dir = TestHelpers.NewTempDir();

    [Fact]
    public void Add_PersistsEntryToFile()
    {
        string file = Path.Combine(_dir, "undo.json");
        var manager = new UndoLogManager(file);
        manager.Add(new RenameEntry(@"C:\old\a.mkv", @"C:\old\b.mkv"));

        string json = File.ReadAllText(file);
        Assert.Contains("Old", json);
        Assert.Contains("New", json);
        Assert.Contains("a.mkv", json);
        Assert.Contains("b.mkv", json);
    }

    [Fact]
    public void Load_RestoresEntries()
    {
        string file = Path.Combine(_dir, "undo.json");
        var manager = new UndoLogManager(file);
        manager.Add(new RenameEntry(@"C:\old\a.mkv", @"C:\old\b.mkv"));
        manager.Add(new RenameEntry(@"C:\old\c.mkv", @"C:\old\d.mkv"));

        var fresh = new UndoLogManager(file);
        fresh.Load();

        Assert.Equal(2, fresh.Log.Count);
        Assert.Equal(@"C:\old\a.mkv", fresh.Log[0].Old);
        Assert.Equal(@"C:\old\b.mkv", fresh.Log[0].New);
    }

    [Fact]
    public void Clear_EmptyPersistedLog()
    {
        string file = Path.Combine(_dir, "undo.json");
        var manager = new UndoLogManager(file);
        manager.Add(new RenameEntry(@"C:\old\a.mkv", @"C:\old\b.mkv"));
        manager.Clear();

        var fresh = new UndoLogManager(file);
        fresh.Load();
        Assert.Empty(fresh.Log);
    }

    [Fact]
    public void Load_MissingFile_Empty()
    {
        var manager = new UndoLogManager(Path.Combine(_dir, "nope.json"));
        manager.Load();
        Assert.Empty(manager.Log);
    }

    [Fact]
    public void Load_CorruptJson_Empty()
    {
        string file = Path.Combine(_dir, "bad.json");
        File.WriteAllText(file, "[[[ not json");
        var manager = new UndoLogManager(file);
        manager.Load();
        Assert.Empty(manager.Log);
    }

    [Fact]
    public void Save_WritesUtf8Bom()
    {
        string file = Path.Combine(_dir, "bom.json");
        var manager = new UndoLogManager(file);
        manager.Add(new RenameEntry("a", "b"));

        byte[] bytes = File.ReadAllBytes(file);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
    }
}