#if !WINDOWS
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using Tmds.DBus;

namespace Resona.Services;

/// <summary>Publishes Resona as an MPRIS player on the Linux session bus.</summary>
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
    public event Action<double>? VolumeRequested;
    public event Action<bool>? ShuffleRequested;
    public event Action<string>? LoopStatusRequested;
    public event Action<Uri>? OpenUriRequested;

    public WindowsMediaSession()
    {
        _mprisObject.CommandRequested += command =>
            Dispatcher.UIThread.Post(() => Pressed?.Invoke(command));
        _mprisObject.SeekRequested += offset =>
            Dispatcher.UIThread.Post(() => SeekRequested?.Invoke(offset));
        _mprisObject.PositionRequested += position =>
            Dispatcher.UIThread.Post(() => PositionRequested?.Invoke(position));
        _mprisObject.VolumeRequested += volume =>
            Dispatcher.UIThread.Post(() => VolumeRequested?.Invoke(volume));
        _mprisObject.ShuffleRequested += shuffle =>
            Dispatcher.UIThread.Post(() => ShuffleRequested?.Invoke(shuffle));
        _mprisObject.LoopStatusRequested += status =>
            Dispatcher.UIThread.Post(() => LoopStatusRequested?.Invoke(status));
        _mprisObject.OpenUriRequested += uri =>
            Dispatcher.UIThread.Post(() => OpenUriRequested?.Invoke(uri));
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
        TimeSpan duration,
        string? filePath = null,
        string? artworkUri = null,
        bool canGoNext = true,
        bool canGoPrevious = true) =>
        _mprisObject.UpdateMetadata(
            trackId, title, artist, position, duration, filePath, artworkUri, canGoNext, canGoPrevious);

    public void UpdatePosition(TimeSpan position, TimeSpan duration) =>
        _mprisObject.UpdatePosition(position, duration);

    public void UpdateVolume(double volume) => _mprisObject.UpdateVolume(volume);
    public void UpdateShuffle(bool shuffle) => _mprisObject.UpdateShuffle(shuffle);
    public void UpdateLoopStatus(string status) => _mprisObject.UpdateLoopStatus(status);
    public void NotifySeeked(TimeSpan position) => _mprisObject.NotifySeeked(position);

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
        [Property(Access = PropertyAccess.Read)] public bool CanSetFullscreen = false;
        [Property(Access = PropertyAccess.Read)] public bool HasTrackList = false;
        [Property(Access = PropertyAccess.Read)] public string Identity = "Resona";
        [Property(Access = PropertyAccess.Read)] public string DesktopEntry = "Resona";
        [Property(Access = PropertyAccess.Read)] public string[] SupportedUriSchemes = ["file"];
        [Property(Access = PropertyAccess.Read)] public string[] SupportedMimeTypes =
            ["audio/mpeg", "audio/mp4", "audio/flac", "audio/wav"];
        [Property(Access = PropertyAccess.ReadWrite)] public bool Fullscreen = false;
    }

    [Tmds.DBus.Dictionary]
    private sealed class PlayerProperties
    {
        [Property(Access = PropertyAccess.Read)] public string PlaybackStatus = "Stopped";
        [Property(Access = PropertyAccess.ReadWrite)] public string LoopStatus = "None";
        [Property(Access = PropertyAccess.ReadWrite)] public double Rate = 1;
        [Property(Access = PropertyAccess.ReadWrite)] public bool Shuffle = false;
        [Property(Access = PropertyAccess.Read)] public IDictionary<string, object> Metadata =
            MprisObject.EmptyMetadata();
        [Property(Access = PropertyAccess.ReadWrite)] public double Volume = 1;
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
        Task SetAsync(string property, object value);
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
        Task<IDisposable> WatchSeekedAsync(Action<long> handler);
        Task<object> GetAsync(string property);
        Task SetAsync(string property, object value);
        Task<IDictionary<string, object>> GetAllAsync();
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    private sealed class MprisObject : IMprisRoot, IMprisPlayer
    {
        private readonly RootProperties _root = new();
        private readonly PlayerProperties _player = new();
        private event Action<PropertyChanges>? PlayerPropertiesChanged;
        private event Action<long>? Seeked;
        private EngineState _state = EngineState.Stopped;
        private int _trackId = -1;
        private string _title = string.Empty;
        private string? _artist;
        private TimeSpan _duration;
        private string? _filePath;
        private string? _artworkUri;

        public ObjectPath ObjectPath { get; } = new("/org/mpris/MediaPlayer2");

        public event Action<MediaShortcut>? CommandRequested;
        public event Action<TimeSpan>? SeekRequested;
        public event Action<TimeSpan>? PositionRequested;
        public event Action<double>? VolumeRequested;
        public event Action<bool>? ShuffleRequested;
        public event Action<string>? LoopStatusRequested;
        public event Action<Uri>? OpenUriRequested;

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
            var target = _player.Position + offset;
            if (target > Microseconds(_duration))
                CommandRequested?.Invoke(MediaShortcut.Next);
            else
            {
                var clampedTarget = Math.Max(0, target);
                SeekRequested?.Invoke(TimeSpan.FromTicks((clampedTarget - _player.Position) * 10));
            }
            return Task.CompletedTask;
        }

        public Task SetPositionAsync(ObjectPath trackId, long position)
        {
            if (_trackId >= 0 && trackId == TrackPath(_trackId)
                && position >= 0 && position <= Microseconds(_duration))
                PositionRequested?.Invoke(TimeSpan.FromTicks(position * 10));
            return Task.CompletedTask;
        }

        public Task OpenUriAsync(string uri)
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsFile)
                OpenUriRequested?.Invoke(parsed);
            return Task.CompletedTask;
        }

        public Task<IDisposable> WatchSeekedAsync(Action<long> handler) =>
            Watch(handler, value => Seeked += value, value => Seeked -= value);

        Task<object> IMprisRoot.GetAsync(string property) =>
            Task.FromResult(RootProperty(property));

        Task IMprisRoot.SetAsync(string property, object value)
        {
            if (property == nameof(RootProperties.Fullscreen) && value is bool)
                throw new DBusException("org.freedesktop.DBus.Error.NotSupported", "Fullscreen control is not supported");
            throw ReadOnlyProperty(property);
        }

        Task<IDictionary<string, object>> IMprisRoot.GetAllAsync() =>
            Task.FromResult(RootPropertyDictionary());

        Task<IDisposable> IMprisRoot.WatchPropertiesAsync(Action<PropertyChanges> handler) =>
            Task.FromResult<IDisposable>(new DelegateDisposable(() => { }));

        Task<object> IMprisPlayer.GetAsync(string property) =>
            Task.FromResult(PlayerProperty(property));

        Task IMprisPlayer.SetAsync(string property, object value)
        {
            switch (property)
            {
                case nameof(PlayerProperties.LoopStatus) when value is string status
                    && status is "None" or "Track" or "Playlist":
                    LoopStatusRequested?.Invoke(status);
                    break;
                case nameof(PlayerProperties.Rate) when value is double rate && rate == 1:
                    break;
                case nameof(PlayerProperties.Rate) when value is double rate && rate == 0:
                    if (_state == EngineState.Playing) CommandRequested?.Invoke(MediaShortcut.PlayPause);
                    break;
                case nameof(PlayerProperties.Shuffle) when value is bool shuffle:
                    ShuffleRequested?.Invoke(shuffle);
                    break;
                case nameof(PlayerProperties.Volume) when value is double volume:
                    VolumeRequested?.Invoke(Math.Max(0, volume));
                    break;
                case nameof(PlayerProperties.LoopStatus):
                case nameof(PlayerProperties.Rate):
                case nameof(PlayerProperties.Shuffle):
                case nameof(PlayerProperties.Volume):
                    throw new DBusException("org.freedesktop.DBus.Error.InvalidArgs", $"Invalid value for {property}");
                default:
                    throw ReadOnlyProperty(property);
            }

            return Task.CompletedTask;
        }

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
                UpdateMetadata(-1, string.Empty, null, TimeSpan.Zero, TimeSpan.Zero, null, null, false, false);
            }
        }

        public void UpdateMetadata(
            int trackId,
            string title,
            string? artist,
            TimeSpan position,
            TimeSpan duration,
            string? filePath,
            string? artworkUri,
            bool canGoNext,
            bool canGoPrevious)
        {
            var metadataChanged = _trackId != trackId
                                  || !string.Equals(_title, title, StringComparison.Ordinal)
                                  || !string.Equals(_artist, artist, StringComparison.Ordinal)
                                  || _duration != duration
                                  || !string.Equals(_filePath, filePath, StringComparison.Ordinal)
                                  || !string.Equals(_artworkUri, artworkUri, StringComparison.Ordinal);
            _trackId = trackId;
            _title = title;
            _artist = artist;
            _duration = duration;
            _filePath = filePath;
            _artworkUri = artworkUri;
            _player.Position = Microseconds(position);
            UpdateCapability(nameof(PlayerProperties.CanGoNext), ref _player.CanGoNext, canGoNext);
            UpdateCapability(nameof(PlayerProperties.CanGoPrevious), ref _player.CanGoPrevious, canGoPrevious);
            if (!metadataChanged)
                return;

            _player.Metadata = BuildMetadata(trackId, title, artist, duration, filePath, artworkUri);
            EmitPlayerChange("Metadata", _player.Metadata);
        }

        public void UpdatePosition(TimeSpan position, TimeSpan duration)
        {
            _player.Position = Microseconds(position);
            if (_duration == duration)
                return;

            _duration = duration;
            _player.Metadata = BuildMetadata(_trackId, _title, _artist, duration, _filePath, _artworkUri);
            EmitPlayerChange("Metadata", _player.Metadata);
        }

        public void UpdateVolume(double volume)
        {
            volume = Math.Max(0, volume);
            if (Math.Abs(_player.Volume - volume) < 0.0001) return;
            _player.Volume = volume;
            EmitPlayerChange(nameof(PlayerProperties.Volume), volume);
        }

        public void UpdateShuffle(bool shuffle)
        {
            if (_player.Shuffle == shuffle) return;
            _player.Shuffle = shuffle;
            EmitPlayerChange(nameof(PlayerProperties.Shuffle), shuffle);
        }

        public void UpdateLoopStatus(string status)
        {
            if (status is not ("None" or "Track" or "Playlist") || _player.LoopStatus == status) return;
            _player.LoopStatus = status;
            EmitPlayerChange(nameof(PlayerProperties.LoopStatus), status);
        }

        public void NotifySeeked(TimeSpan position)
        {
            _player.Position = Microseconds(position);
            Seeked?.Invoke(_player.Position);
        }

        public static IDictionary<string, object> EmptyMetadata() =>
            new Dictionary<string, object>();

        private static IDictionary<string, object> BuildMetadata(
            int trackId,
            string title,
            string? artist,
            TimeSpan duration,
            string? filePath,
            string? artworkUri)
        {
            var metadata = new Dictionary<string, object>
            {
                ["mpris:trackid"] = TrackPath(trackId),
                ["mpris:length"] = Microseconds(duration),
                ["xesam:title"] = string.IsNullOrWhiteSpace(title) ? "Unknown track" : title,
                ["xesam:artist"] = string.IsNullOrWhiteSpace(artist) ? new[] { "Unknown artist" } : new[] { artist }
            };
            if (!string.IsNullOrWhiteSpace(artworkUri)
                && Uri.TryCreate(artworkUri, UriKind.Absolute, out var artwork)
                && artwork.IsFile)
                metadata["mpris:artUrl"] = artwork.AbsoluteUri;
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                try { metadata["xesam:url"] = new Uri(Path.GetFullPath(filePath)).AbsoluteUri; }
                catch { }
            }
            return metadata;
        }

        private object RootProperty(string property) => property switch
        {
            nameof(RootProperties.CanQuit) => _root.CanQuit,
            nameof(RootProperties.CanRaise) => _root.CanRaise,
            nameof(RootProperties.CanSetFullscreen) => _root.CanSetFullscreen,
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
            [nameof(RootProperties.CanSetFullscreen)] = _root.CanSetFullscreen,
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

        private void UpdateCapability(string property, ref bool target, bool value)
        {
            if (target == value) return;
            target = value;
            EmitPlayerChange(property, value);
        }

        private static Task<IDisposable> Watch<T>(
            Action<T> handler,
            Action<Action<T>> add,
            Action<Action<T>> remove)
        {
            add(handler);
            return Task.FromResult<IDisposable>(new DelegateDisposable(() => remove(handler)));
        }

        private static DBusException ReadOnlyProperty(string property) =>
            new("org.freedesktop.DBus.Error.PropertyReadOnly", $"Property {property} is read-only");

        private static long Microseconds(TimeSpan value) =>
            Math.Max(0, value.Ticks / 10);

        private static ObjectPath TrackPath(int trackId) =>
            new(trackId < 0
                ? "/org/mpris/MediaPlayer2/TrackList/NoTrack"
                : $"/com/beranmusic/Track/{trackId}");
    }

    private sealed class DelegateDisposable(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() => System.Threading.Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
#endif
