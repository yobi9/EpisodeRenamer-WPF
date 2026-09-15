using System.IO;
using System.Text;
using System.Text.Json;

namespace EpisodeRenamer.Core;

public sealed class UndoLogManager
{
    private readonly List<RenameEntry> _log = new();

    public string UndoPath { get; }

    public IReadOnlyList<RenameEntry> Log => _log;

    public UndoLogManager(string undoPath)
    {
        UndoPath = undoPath;
    }

    public void Load()
    {
        _log.Clear();
        if (!File.Exists(UndoPath)) return;
        try
        {
            var loaded = JsonSerializer.Deserialize<List<RenameEntry>>(File.ReadAllText(UndoPath));
            if (loaded is not null) _log.AddRange(loaded);
        }
        catch
        {
            _log.Clear();
        }
    }

    public void Add(RenameEntry entry)
    {
        _log.Add(entry);
        Save();
    }

    public void Clear()
    {
        _log.Clear();
        Save();
    }

    public IReadOnlyList<(RenameEntry Entry, bool Success)> UndoAll()
    {
        var results = new List<(RenameEntry, bool)>();
        foreach (RenameEntry item in _log.ToList())
        {
            if (item.New is not null && item.Old is not null && File.Exists(item.New))
            {
                try
                {
                    string dir = Path.GetDirectoryName(item.New) ?? "";
                    string oldName = Path.GetFileName(item.Old);
                    File.Move(item.New, Path.Combine(dir, oldName));
                    results.Add((item, true));
                    continue;
                }
                catch
                {
                }
            }
            results.Add((item, false));
        }
        _log.Clear();
        Save();
        return results;
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(UndoPath, JsonSerializer.Serialize(_log), new UTF8Encoding(true));
        }
        catch
        {
        }
    }
}