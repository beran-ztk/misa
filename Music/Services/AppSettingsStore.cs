using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Music.Services;

public static class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(Values.AppSettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(Values.AppSettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            settings.Volume = Math.Clamp(settings.Volume, 0f, 1f);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void SaveVolume(float volume)
    {
        var settings = Load();
        settings.Volume = Math.Clamp(volume, 0f, 1f);
        Save(settings);
    }

    public static void SaveMusicAnalysisServerUrl(string serverUrl)
    {
        var settings = Load();
        settings.MusicAnalysisServerUrl = serverUrl;
        Save(settings);
    }

    private static void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(Values.AppSettingsPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(Values.AppSettingsPath, json);
    }
}

public sealed class AppSettings
{
    public float Volume { get; set; } = 1f;
    public string MusicAnalysisServerUrl { get; set; } = string.Empty;
}
