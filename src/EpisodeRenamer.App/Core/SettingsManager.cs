using System.IO;
using System.Text;
using System.Text.Json;

namespace EpisodeRenamer.Core;

public sealed class SettingsManager
{
    public string SettingsPath { get; }

    public SettingsManager(string settingsPath)
    {
        SettingsPath = settingsPath;
    }

    public void Save(AppSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings);
            File.WriteAllText(SettingsPath, json, new UTF8Encoding(true));
        }
        catch
        {
        }
    }

    public AppSettings? Load()
    {
        if (!File.Exists(SettingsPath)) return null;
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
        }
        catch
        {
            return null;
        }
    }

    public void SaveTo(string path, AppSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json, new UTF8Encoding(true));
        }
        catch
        {
        }
    }

    public AppSettings? LoadFrom(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }
}