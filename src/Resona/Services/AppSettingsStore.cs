using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Resona.Models;

namespace Resona.Services;

public static class AppSettingsStore
{
    public const int ChannelDownloadMinDurationMinutes = 1;
    public const int ChannelDownloadMaxDurationMinutes = 180;

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
            settings.ChannelDownloadMaxDurationMinutes = Math.Clamp(
                settings.ChannelDownloadMaxDurationMinutes,
                ChannelDownloadMinDurationMinutes,
                ChannelDownloadMaxDurationMinutes);
            settings.Appearance = (settings.Appearance ?? new AppearanceSettings()).Clamp();
            settings.PlayerSession ??= new PlayerSessionSettings();
            settings.PlayerSession.QueueTrackIds ??= [];
            settings.TrackBackdropFocus = settings.TrackBackdropFocus?
                .ToDictionary(
                    pair => pair.Key,
                    pair => Math.Clamp(pair.Value, 0d, 1d)) ?? [];
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

    public static void SaveMusicAnalysisServerConfiguration(string serverUrl, string? apiKey)
    {
        var settings = Load();
        settings.MusicAnalysisServerUrl = serverUrl;
        settings.MusicAnalysisApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
        Save(settings);
    }

    public static void SaveChannelDownloadMaxDurationMinutes(int minutes)
    {
        var settings = Load();
        settings.ChannelDownloadMaxDurationMinutes = Math.Clamp(
            minutes,
            ChannelDownloadMinDurationMinutes,
            ChannelDownloadMaxDurationMinutes);
        Save(settings);
    }

    public static void SaveAppearance(AppearanceSettings appearance)
    {
        var settings = Load();
        settings.Appearance = appearance.Clone().Clamp();
        Save(settings);
    }

    public static void SaveLastSettingsPage(string page)
    {
        var settings = Load();
        settings.LastSettingsPage = page;
        Save(settings);
    }

    public static void SaveDiscordPresence(bool enabled, string? stateText, string? largeImageText)
    {
        var settings = Load();
        settings.DiscordRichPresenceEnabled = enabled;
        settings.DiscordStateText = string.IsNullOrEmpty(stateText) ? null : stateText;
        settings.DiscordLargeImageText = string.IsNullOrEmpty(largeImageText) ? null : largeImageText;
        Save(settings);
    }

    public static void SaveCloudServerUrl(string? serverUrl)
    {
        var settings = Load();
        settings.CloudServerUrl = string.IsNullOrWhiteSpace(serverUrl) ? null : serverUrl.Trim();
        Save(settings);
    }

    public static void SavePlayerSession(PlayerSessionSettings playerSession)
    {
        var settings = Load();
        settings.PlayerSession = playerSession;
        Save(settings);
    }

    public static void SaveTrackBackdropFocus(int trackId, double focusX)
    {
        var settings = Load();
        settings.TrackBackdropFocus[trackId] = Math.Clamp(focusX, 0d, 1d);
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
    public float Volume { get; set; } = 0.53030306f;
    public string MusicAnalysisServerUrl { get; set; } = "https://analyzer.resona-music.de";
    public string? MusicAnalysisApiKey { get; set; }
    public int ChannelDownloadMaxDurationMinutes { get; set; } = 12;
    public AppearanceSettings Appearance { get; set; } = new();
    public string LastSettingsPage { get; set; } = "library";
    public PlayerSessionSettings PlayerSession { get; set; } = new();
    public Dictionary<int, double> TrackBackdropFocus { get; set; } = [];
    public string? DiscordStateText { get; set; }
    public string? DiscordLargeImageText { get; set; }
    public bool DiscordRichPresenceEnabled { get; set; } = true;
    public string? CloudServerUrl { get; set; } = "https://api.resona-music.de";
}

public sealed class PlayerSessionSettings
{
    public string? ActiveFilterPresetName { get; set; }
    public int? ActiveTrackId { get; set; }
    public int? SelectedTrackId { get; set; }
    public bool ShuffleEnabled { get; set; }
    public string SortBy { get; set; } = "Name";
    public string SortDirection { get; set; } = "Ascending";
    public List<int> QueueTrackIds { get; set; } = [];
}
