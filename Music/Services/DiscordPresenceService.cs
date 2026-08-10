using System;
using DiscordRPC;
using Music.Models;

namespace Music.Services;

public sealed class DiscordPresenceService : IDisposable
{
    private const string ClientId = "1524163394276425728";
    private const string LargeImageUrl =
        "https://raw.githubusercontent.com/beran-ztk/music/master/Music/Assets/music.png";
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    private DiscordRpcClient? _client;
    private DateTime _nextConnectAttemptUtc = DateTime.MinValue;
    private bool _disposed;
    private int _lastTrackId = -1;

    public void Update(TrackDisplayItem item, EngineState state, TimeSpan currentTime, TimeSpan totalTime)
    {
        if (_disposed || state == EngineState.Stopped)
        {
            Clear();
            return;
        }

        if (!EnsureClient())
            return;

        var presence = new RichPresence
        {
            Type = ActivityType.Listening,
            StatusDisplay = StatusDisplayType.Details,
            Details = Clip(PresenceTitle(item), 128),
            State = ClipOrNull(StateText(item), 128),
            Assets = new Assets
            {
                LargeImageKey = LargeImageUrl
                // LargeImageText = "Music"
            }
        };

        if (state == EngineState.Playing && totalTime > TimeSpan.Zero)
        {
            var now = DateTime.UtcNow;
            var start = now - currentTime;
            var end = now + (totalTime - currentTime);
            if (end > start)
                presence.Timestamps = new Timestamps(start, end);
        }

        TrySend(() =>
        {
            if (_lastTrackId != item.Track.Id)
            {
                _client!.ClearPresence();
                _lastTrackId = item.Track.Id;
            }

            _client!.SetPresence(presence);
        });
    }

    public void Clear()
    {
        if (_client is null)
            return;

        TrySend(() => _client.ClearPresence());
        _lastTrackId = -1;
    }

    private bool EnsureClient()
    {
        if (_client?.IsInitialized == true)
            return true;

        var now = DateTime.UtcNow;
        if (now < _nextConnectAttemptUtc)
            return false;

        _nextConnectAttemptUtc = now + ReconnectDelay;
        DisposeClient();

        try
        {
            _client = new DiscordRpcClient(ClientId)
            {
                SkipIdenticalPresence = true
            };
            return _client.Initialize();
        }
        catch
        {
            DisposeClient();
            return false;
        }
    }

    private void TrySend(Action send)
    {
        try
        {
            send();
        }
        catch
        {
            DisposeClient();
        }
    }

    private static string? StateText(TrackDisplayItem item)
    {
        return !string.IsNullOrWhiteSpace(item.ModelGenreText)
            ? item.ModelGenreText
            : !string.IsNullOrWhiteSpace(item.ManualGenreText)
                ? item.ManualGenreText
                : null;
    }

    private static string PresenceTitle(TrackDisplayItem item)
    {
        var title = string.IsNullOrWhiteSpace(item.Track.Title)
            ? "Unknown track"
            : item.Track.Title.Trim();

        return string.IsNullOrWhiteSpace(item.ChannelText)
            ? title
            : $"{title} - {item.ChannelText.Trim()}";
    }

    private static string Clip(string value, int maxLength)
    {
        value = string.IsNullOrWhiteSpace(value) ? "Unknown track" : value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? ClipOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private void DisposeClient()
    {
        if (_client is null)
            return;

        try { _client.ClearPresence(); } catch { }
        try { _client.Dispose(); } catch { }
        _client = null;
        _lastTrackId = -1;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        DisposeClient();
    }
}
