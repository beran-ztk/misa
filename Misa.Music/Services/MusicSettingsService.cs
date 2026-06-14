using System;
using System.IO;
using System.Text.Json;
using Misa.Music.Models;

namespace Misa.Music.Services;

public static class MusicSettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Misa", "music-settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static MusicSettings GetDefaultSettings() => new()
    {
        MusicDirectory = @"D:\media\music",
        ToolsDirectory = @"D:\media\tools",
        UseNodeJsRuntime = true,
        UseFirefoxCookies = false,
        UseRemoteEjsComponents = false,
        Volume = 100,
        IsMuted = false,
        ShuffleEnabled = false,
        AutoplayEnabled = false,
        RepeatMode = "None",
    };

    public static MusicSettings LoadSettings()
    {
        EnsureSettingsExist();
        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<MusicSettings>(json) ?? GetDefaultSettings();
        }
        catch
        {
            return GetDefaultSettings();
        }
    }

    public static void SaveSettings(MusicSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static void EnsureSettingsExist()
    {
        if (!File.Exists(SettingsPath))
            SaveSettings(GetDefaultSettings());
    }
}
