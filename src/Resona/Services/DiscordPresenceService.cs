using System;
using DiscordRPC;
using Resona.Models;

namespace Resona.Services;

public sealed class DiscordPresenceService : IDisposable
{
    private const string ClientId = "1524163394276425728";
    private const string FallbackImageUrl =
        "https://raw.githubusercontent.com/bezztk/resona/master/src/Resona/Assets/headphones.png";
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    private DiscordRpcClient? _client;
    private DateTime _nextConnectAttemptUtc = DateTime.MinValue;
    private bool _disposed;
    private int _lastTrackId = -1;
    private string? _largeImageText;
    private bool _enabled = true;

    public DiscordPresenceService() => ReloadSettings();

    public void ReloadSettings()
    {
        var settings = AppSettingsStore.Load();
        _enabled = settings.DiscordRichPresenceEnabled;
        _largeImageText = string.IsNullOrEmpty(settings.DiscordLargeImageText) ? null : settings.DiscordLargeImageText;
        if (!_enabled)
            DisposeClient();
    }

    public void Update(TrackDisplayItem item, EngineState state, TimeSpan currentTime, TimeSpan totalTime)
    {
        if (_disposed || !_enabled || state == EngineState.Stopped)
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
            DetailsUrl = PresenceUrl(item.Track.CanonicalUrl),
            State = ClipPreservingWhitespaceOrNull(ArtistText(item), 128),
            StateUrl = PresenceUrl(item.Track.ChannelUrl),

            Assets = new Assets
            {
                LargeImageKey = TrackImageUrl(item.Track),
                LargeImageText = ClipPreservingWhitespaceOrNull(_largeImageText, 128)
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

    private static string? GenreText(TrackDisplayItem item)
    {
        return !string.IsNullOrWhiteSpace(item.ModelGenreText)
            ? item.ModelGenreText
            : !string.IsNullOrWhiteSpace(item.ManualGenreText)
                ? item.ManualGenreText
                : null;
    }

    private static string? ArtistText(TrackDisplayItem item) =>
        string.IsNullOrWhiteSpace(item.ChannelText) ? null : item.ChannelText.Trim();

    private static string PresenceTitle(TrackDisplayItem item) =>
        string.IsNullOrWhiteSpace(item.Track.Title)
            ? "Unknown track"
            : item.Track.Title.Trim();

    private static string? PresenceUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > 256
            || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        return uri.AbsoluteUri;
    }

    private static string TrackImageUrl(MusicTrack track)
    {
        var videoId = YouTubeUrlNormalizer.ExtractVideoId(track.CanonicalUrl);
        return string.IsNullOrWhiteSpace(videoId)
            ? FallbackImageUrl
            : $"https://wsrv.nl/?url=i.ytimg.com/vi/{Uri.EscapeDataString(videoId)}/mqdefault.jpg&w=512&h=512&fit=cover&a=center&output=jpg&default=1";
    }

    private static string Clip(string value, int maxLength)
    {
        value = string.IsNullOrWhiteSpace(value) ? "Unknown track" : value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? ClipPreservingWhitespaceOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return null;

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
