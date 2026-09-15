using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EpisodeRenamer.Core;

public sealed class SettingsManager
{
    public string SettingsPath { get; }

    public SettingsManager(string settingsPath)
    {
        SettingsPath = settingsPath;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public void Save(AppSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, JsonOptions);
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
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions);
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
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}