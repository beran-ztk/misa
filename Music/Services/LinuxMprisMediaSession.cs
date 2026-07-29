#if !WINDOWS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using Tmds.DBus;

namespace Music.Services;

/// <summary>Publishes Music as an MPRIS player on the Linux session bus.</summary>
public sealed class WindowsMediaSession : IDisposable
{
    private const string ServiceName = "org.mpris.MediaPlayer2.BeranMusic";
    private readonly MprisObject _mprisObject = new();
    private Connection? _connection;
    private bool _started;
    private bool _disposed;

    public event Action<MediaShortcut>? Pressed;
    public event Action<TimeSpan>? SeekRequested;
    public event Action<TimeSpan>? PositionRequested;

    public WindowsMediaSession()
    {
        _mprisObject.CommandRequested += command =>
            Dispatcher.UIThread.Post(() => Pressed?.Invoke(command));
        _mprisObject.SeekRequested += offset =>
            Dispatcher.UIThread.Post(() => SeekRequested?.Invoke(offset));
        _mprisObject.PositionRequested += position =>
            Dispatcher.UIThread.Post(() => PositionRequested?.Invoke(position));
    }

    public bool Start()
    {
        if (_disposed)
            return false;
        if (_started)
            return true;

        _started = true;
        _ = StartAsync();
        return true;
    }

    public void UpdateState(EngineState state) => _mprisObject.UpdateState(state);

    public void UpdateMetadata(
        int trackId,
        string title,
        string? artist,
        TimeSpan position,
        TimeSpan duration) =>
        _mprisObject.UpdateMetadata(trackId, title, artist, position, duration);

    public void UpdatePosition(TimeSpan position, TimeSpan duration) =>
        _mprisObject.UpdatePosition(position, duration);

    private async Task StartAsync()
    {
        try
        {
            var address = Address.Session;
            if (string.IsNullOrWhiteSpace(address) || _disposed)
                return;

            var connection = new Connection(address);
            await connection.ConnectAsync();
            if (_disposed)
            {
                connection.Dispose();
                return;
            }

            await connection.RegisterServiceAsync(ServiceName, ServiceRegistrationOptions.Default);
            await connection.RegisterObjectAsync(_mprisObject);
            _connection = connection;
        }
        catch
        {
            // Desktop media integration is optional; playback must remain available without D-Bus.
            _started = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        var connection = _connection;
        _connection = null;
        if (connection is null)
            return;

        try { connection.UnregisterObject(_mprisObject); } catch { }
        try { connection.Dispose(); } catch { }
    }

    [Tmds.DBus.Dictionary]
    private sealed class RootProperties
    {
        [Property(Access = PropertyAccess.Read)] public bool CanQuit = false;
        [Property(Access = PropertyAccess.Read)] public bool CanRaise = false;
        [Property(Access = PropertyAccess.Read)] public bool HasTrackList = false;
        [Property(Access = PropertyAccess.Read)] public string Identity = "Music";
        [Property(Access = PropertyAccess.Read)] public string DesktopEntry = "Beran.Music";
        [Property(Access = PropertyAccess.Read)] public string[] SupportedUriSchemes = ["file"];
        [Property(Access = PropertyAccess.Read)] public string[] SupportedMimeTypes =
            ["audio/mpeg", "audio/mp4", "audio/flac", "audio/wav"];
        [Property(Access = PropertyAccess.Read)] public bool Fullscreen = false;
    }

    [Tmds.DBus.Dictionary]
    private sealed class PlayerProperties
    {
        [Property(Access = PropertyAccess.Read)] public string PlaybackStatus = "Stopped";
        [Property(Access = PropertyAccess.Read)] public string LoopStatus = "None";
        [Property(Access = PropertyAccess.Read)] public double Rate = 1;
        [Property(Access = PropertyAccess.Read)] public bool Shuffle = false;
        [Property(Access = PropertyAccess.Read)] public IDictionary<string, object> Metadata =
            MprisObject.EmptyMetadata();
        [Property(Access = PropertyAccess.Read)] public double Volume = 1;
        [Property(Access = PropertyAccess.Read)] public long Position = 0;
        [Property(Access = PropertyAccess.Read)] public double MinimumRate = 1;
        [Property(Access = PropertyAccess.Read)] public double MaximumRate = 1;
        [Property(Access = PropertyAccess.Read)] public bool CanGoNext = true;
        [Property(Access = PropertyAccess.Read)] public bool CanGoPrevious = true;
        [Property(Access = PropertyAccess.Read)] public bool CanPlay = true;
        [Property(Access = PropertyAccess.Read)] public bool CanPause = true;
        [Property(Access = PropertyAccess.Read)] public bool CanSeek = true;
        [Property(Access = PropertyAccess.Read)] public bool CanControl = true;
    }

    [DBusInterface("org.mpris.MediaPlayer2", PropertyType = typeof(RootProperties))]
    private interface IMprisRoot : IDBusObject
    {
        Task RaiseAsync();
        Task QuitAsync();
        Task<object> GetAsync(string property);
        Task<IDictionary<string, object>> GetAllAsync();
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [DBusInterface("org.mpris.MediaPlayer2.Player", PropertyType = typeof(PlayerProperties))]
    private interface IMprisPlayer : IDBusObject
    {
        Task NextAsync();
        Task PreviousAsync();
        Task PauseAsync();
        Task PlayPauseAsync();
        Task StopAsync();
        Task PlayAsync();
        Task SeekAsync(long offset);
        Task SetPositionAsync(ObjectPath trackId, long position);
        Task OpenUriAsync(string uri);
        Task<object> GetAsync(string property);
        Task<IDictionary<string, object>> GetAllAsync();
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    private sealed class MprisObject : IMprisRoot, IMprisPlayer
    {
        private readonly RootProperties _root = new();
        private readonly PlayerProperties _player = new();
        private event Action<PropertyChanges>? PlayerPropertiesChanged;
        private EngineState _state = EngineState.Stopped;
        private int _trackId = -1;
        private string _title = string.Empty;
        private string? _artist;
        private TimeSpan _duration;

        public ObjectPath ObjectPath { get; } = new("/org/mpris/MediaPlayer2");

        public event Action<MediaShortcut>? CommandRequested;
        public event Action<TimeSpan>? SeekRequested;
        public event Action<TimeSpan>? PositionRequested;

        public Task RaiseAsync() => Task.CompletedTask;
        public Task QuitAsync() => Task.CompletedTask;
        public Task NextAsync() => Command(MediaShortcut.Next);
        public Task PreviousAsync() => Command(MediaShortcut.Previous);
        public Task PlayPauseAsync() => Command(MediaShortcut.PlayPause);
        public Task StopAsync() => Command(MediaShortcut.Stop);

        public Task PauseAsync() =>
            _state == EngineState.Playing ? Command(MediaShortcut.PlayPause) : Task.CompletedTask;

        public Task PlayAsync() =>
            _state == EngineState.Playing ? Task.CompletedTask : Command(MediaShortcut.PlayPause);

        public Task SeekAsync(long offset)
        {
            SeekRequested?.Invoke(TimeSpan.FromTicks(offset * 10));
            return Task.CompletedTask;
        }

        public Task SetPositionAsync(ObjectPath trackId, long position)
        {
            if (trackId == TrackPath(_trackId))
                PositionRequested?.Invoke(TimeSpan.FromTicks(Math.Max(0, position) * 10));
            return Task.CompletedTask;
        }

        public Task OpenUriAsync(string uri)
        {
            _ = uri;
            return Task.CompletedTask;
        }

        Task<object> IMprisRoot.GetAsync(string property) =>
            Task.FromResult(RootProperty(property));

        Task<IDictionary<string, object>> IMprisRoot.GetAllAsync() =>
            Task.FromResult(RootPropertyDictionary());

        Task<IDisposable> IMprisRoot.WatchPropertiesAsync(Action<PropertyChanges> handler) =>
            Task.FromResult<IDisposable>(new DelegateDisposable(() => { }));

        Task<object> IMprisPlayer.GetAsync(string property) =>
            Task.FromResult(PlayerProperty(property));

        Task<IDictionary<string, object>> IMprisPlayer.GetAllAsync() =>
            Task.FromResult(PlayerPropertyDictionary());

        Task<IDisposable> IMprisPlayer.WatchPropertiesAsync(Action<PropertyChanges> handler) =>
            Watch(
                handler,
                add: value => PlayerPropertiesChanged += value,
                remove: value => PlayerPropertiesChanged -= value);

        public void UpdateState(EngineState state)
        {
            _state = state;
            var status = state switch
            {
                EngineState.Playing => "Playing",
                EngineState.Paused => "Paused",
                _ => "Stopped"
            };
            if (_player.PlaybackStatus == status)
                return;

            _player.PlaybackStatus = status;
            EmitPlayerChange("PlaybackStatus", status);
            if (state == EngineState.Stopped)
            {
                _player.Position = 0;
                UpdateMetadata(-1, string.Empty, null, TimeSpan.Zero, TimeSpan.Zero);
            }
        }

        public void UpdateMetadata(
            int trackId,
            string title,
            string? artist,
            TimeSpan position,
            TimeSpan duration)
        {
            var metadataChanged = _trackId != trackId
                                  || !string.Equals(_title, title, StringComparison.Ordinal)
                                  || !string.Equals(_artist, artist, StringComparison.Ordinal)
                                  || _duration != duration;
            _trackId = trackId;
            _title = title;
            _artist = artist;
            _duration = duration;
            _player.Position = Microseconds(position);
            if (!metadataChanged)
                return;

            _player.Metadata = BuildMetadata(trackId, title, artist, duration);
            EmitPlayerChange("Metadata", _player.Metadata);
        }

        public void UpdatePosition(TimeSpan position, TimeSpan duration)
        {
            _player.Position = Microseconds(position);
            if (_duration == duration)
                return;

            _duration = duration;
            _player.Metadata = BuildMetadata(_trackId, _title, _artist, duration);
            EmitPlayerChange("Metadata", _player.Metadata);
        }

        public static IDictionary<string, object> EmptyMetadata() =>
            new Dictionary<string, object>
            {
                ["mpris:trackid"] = TrackPath(-1)
            };

        private static IDictionary<string, object> BuildMetadata(
            int trackId,
            string title,
            string? artist,
            TimeSpan duration)
        {
            var metadata = new Dictionary<string, object>
            {
                ["mpris:trackid"] = TrackPath(trackId),
                ["mpris:length"] = Microseconds(duration),
                ["xesam:title"] = string.IsNullOrWhiteSpace(title) ? "Unknown track" : title
            };
            if (!string.IsNullOrWhiteSpace(artist))
                metadata["xesam:artist"] = new[] { artist };
            return metadata;
        }

        private object RootProperty(string property) => property switch
        {
            nameof(RootProperties.CanQuit) => _root.CanQuit,
            nameof(RootProperties.CanRaise) => _root.CanRaise,
            nameof(RootProperties.HasTrackList) => _root.HasTrackList,
            nameof(RootProperties.Identity) => _root.Identity,
            nameof(RootProperties.DesktopEntry) => _root.DesktopEntry,
            nameof(RootProperties.SupportedUriSchemes) => _root.SupportedUriSchemes,
            nameof(RootProperties.SupportedMimeTypes) => _root.SupportedMimeTypes,
            nameof(RootProperties.Fullscreen) => _root.Fullscreen,
            _ => throw new DBusException("org.freedesktop.DBus.Error.InvalidArgs", $"Unknown property {property}")
        };

        private IDictionary<string, object> RootPropertyDictionary() => new Dictionary<string, object>
        {
            [nameof(RootProperties.CanQuit)] = _root.CanQuit,
            [nameof(RootProperties.CanRaise)] = _root.CanRaise,
            [nameof(RootProperties.HasTrackList)] = _root.HasTrackList,
            [nameof(RootProperties.Identity)] = _root.Identity,
            [nameof(RootProperties.DesktopEntry)] = _root.DesktopEntry,
            [nameof(RootProperties.SupportedUriSchemes)] = _root.SupportedUriSchemes,
            [nameof(RootProperties.SupportedMimeTypes)] = _root.SupportedMimeTypes,
            [nameof(RootProperties.Fullscreen)] = _root.Fullscreen
        };

        private object PlayerProperty(string property) => property switch
        {
            nameof(PlayerProperties.PlaybackStatus) => _player.PlaybackStatus,
            nameof(PlayerProperties.LoopStatus) => _player.LoopStatus,
            nameof(PlayerProperties.Rate) => _player.Rate,
            nameof(PlayerProperties.Shuffle) => _player.Shuffle,
            nameof(PlayerProperties.Metadata) => _player.Metadata,
            nameof(PlayerProperties.Volume) => _player.Volume,
            nameof(PlayerProperties.Position) => _player.Position,
            nameof(PlayerProperties.MinimumRate) => _player.MinimumRate,
            nameof(PlayerProperties.MaximumRate) => _player.MaximumRate,
            nameof(PlayerProperties.CanGoNext) => _player.CanGoNext,
            nameof(PlayerProperties.CanGoPrevious) => _player.CanGoPrevious,
            nameof(PlayerProperties.CanPlay) => _player.CanPlay,
            nameof(PlayerProperties.CanPause) => _player.CanPause,
            nameof(PlayerProperties.CanSeek) => _player.CanSeek,
            nameof(PlayerProperties.CanControl) => _player.CanControl,
            _ => throw new DBusException("org.freedesktop.DBus.Error.InvalidArgs", $"Unknown property {property}")
        };

        private IDictionary<string, object> PlayerPropertyDictionary() => new Dictionary<string, object>
        {
            [nameof(PlayerProperties.PlaybackStatus)] = _player.PlaybackStatus,
            [nameof(PlayerProperties.LoopStatus)] = _player.LoopStatus,
            [nameof(PlayerProperties.Rate)] = _player.Rate,
            [nameof(PlayerProperties.Shuffle)] = _player.Shuffle,
            [nameof(PlayerProperties.Metadata)] = _player.Metadata,
            [nameof(PlayerProperties.Volume)] = _player.Volume,
            [nameof(PlayerProperties.Position)] = _player.Position,
            [nameof(PlayerProperties.MinimumRate)] = _player.MinimumRate,
            [nameof(PlayerProperties.MaximumRate)] = _player.MaximumRate,
            [nameof(PlayerProperties.CanGoNext)] = _player.CanGoNext,
            [nameof(PlayerProperties.CanGoPrevious)] = _player.CanGoPrevious,
            [nameof(PlayerProperties.CanPlay)] = _player.CanPlay,
            [nameof(PlayerProperties.CanPause)] = _player.CanPause,
            [nameof(PlayerProperties.CanSeek)] = _player.CanSeek,
            [nameof(PlayerProperties.CanControl)] = _player.CanControl
        };

        private Task Command(MediaShortcut command)
        {
            CommandRequested?.Invoke(command);
            return Task.CompletedTask;
        }

        private void EmitPlayerChange(string property, object value) =>
            PlayerPropertiesChanged?.Invoke(PropertyChanges.ForProperty(property, value));

        private static Task<IDisposable> Watch(
            Action<PropertyChanges> handler,
            Action<Action<PropertyChanges>> add,
            Action<Action<PropertyChanges>> remove)
        {
            add(handler);
            return Task.FromResult<IDisposable>(new DelegateDisposable(() => remove(handler)));
        }

        private static long Microseconds(TimeSpan value) =>
            Math.Max(0, value.Ticks / 10);

        private static ObjectPath TrackPath(int trackId) =>
            new(trackId < 0
                ? "/org/mpris/MediaPlayer2/Track/None"
                : $"/org/mpris/MediaPlayer2/Track/{trackId}");
    }

    private sealed class DelegateDisposable(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() => System.Threading.Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
#endif
