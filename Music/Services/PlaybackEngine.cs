using System;
using Avalonia.Threading;
using NAudio.Wave;

namespace Music.Services;

public enum EngineState { Stopped, Playing, Paused }

// Controls playback of up to two simultaneous tracks (primary + fading-out secondary).
// Volume is controlled entirely in software via AudioFileReader.Volume (sample multiplication)
// so each slot's volume is fully independent — no shared hardware/device volume is touched.
public sealed class PlaybackEngine : IDisposable
{
    private sealed class AudioSlot : IDisposable
    {
        public readonly IWavePlayer Player;
        public readonly AudioFileReader Reader;   // owns CurrentTime, TotalTime, software Volume
        public readonly int TrackId;

        // 0..1 transition factor; Reader.Volume = MasterVolume * TransitionVolume (or 0 if muted).
        public float TransitionVolume;
        public float FadeTarget;   // 0 = fade out fully, 1 = fade in to full
        public float FadeStep;     // per 100 ms tick, signed; 0 = steady

        public AudioSlot(IWavePlayer player, AudioFileReader reader, int trackId, float startTransition)
        {
            Player = player;
            Reader = reader;
            TrackId = trackId;
            TransitionVolume = startTransition;
            FadeTarget = startTransition;
        }

        // Set the effective output level: MasterVolume * TransitionVolume (0 if muted).
        public void ApplySoftVolume(float masterVolume, bool muted) =>
            Reader.Volume = muted ? 0f : Math.Clamp(masterVolume * TransitionVolume, 0f, 1f);

        public void Dispose()
        {
            try { Player.Stop(); } catch { }
            try { Player.Dispose(); } catch { }
            try { Reader.Dispose(); } catch { }
        }
    }

    // Raised on the UI thread (DispatcherTimer guarantees it).
    public event Action? TrackNaturallyEnded;
    public event Action? StateChanged;
    public event Action? ProgressUpdated;

    public EngineState State { get; private set; } = EngineState.Stopped;
    public int ActiveTrackId { get; private set; } = -1;
    public TimeSpan CurrentTime { get; private set; }
    public TimeSpan TotalTime { get; private set; }
    public bool IsCrossfading => _secondary != null;

    // User-facing volume settings (set by MusicView; engine does not persist them).
    public float MasterVolume { get; set; } = 1f;
    public bool Muted { get; set; }

    private AudioSlot? _primary;
    private AudioSlot? _secondary;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private bool _disposed;

    public PlaybackEngine() => _timer.Tick += OnTick;

    // Begin playing filePath.
    // If a track is already playing it fades out over fadeOutSeconds (0 = immediate stop).
    // The new track fades in over fadeInSeconds (0 = starts at full MasterVolume immediately).
    public void Play(string filePath, int trackId, float fadeOutSeconds, float fadeInSeconds)
    {
        if (_disposed) return;

        // Drop any existing secondary immediately — only one outgoing slot at a time.
        if (_secondary != null)
        {
            _secondary.Player.PlaybackStopped -= OnSecondaryEnded;
            _secondary.Dispose();
            _secondary = null;
        }

        // Promote current primary → secondary with a fade-out.
        if (_primary != null)
        {
            _primary.Player.PlaybackStopped -= OnPrimaryEnded;
            _primary.Player.PlaybackStopped += OnSecondaryEnded;
            _secondary = _primary;
            _primary = null;

            if (fadeOutSeconds > 0f && _secondary.TransitionVolume > 0f)
            {
                _secondary.FadeTarget = 0f;
                _secondary.FadeStep = -_secondary.TransitionVolume / (fadeOutSeconds * 10f);
            }
            else
            {
                // Nothing left to fade out — kill it immediately.
                _secondary.Player.PlaybackStopped -= OnSecondaryEnded;
                _secondary.Dispose();
                _secondary = null;
            }
        }

        // Create new primary slot.
        var reader = new AudioFileReader(filePath);
        var player = new WaveOutEvent();
        player.PlaybackStopped += OnPrimaryEnded;
        player.Init(reader);

        float startTransition = fadeInSeconds > 0f ? 0f : 1f;
        _primary = new AudioSlot(player, reader, trackId, startTransition)
        {
            FadeTarget = 1f,
            FadeStep = fadeInSeconds > 0f ? 1f / (fadeInSeconds * 10f) : 0f,
        };
        _primary.ApplySoftVolume(MasterVolume, Muted);

        player.Play();
        ActiveTrackId = trackId;
        State = EngineState.Playing;
        _timer.Start();
        StateChanged?.Invoke();
    }

    public void Pause()
    {
        if (_primary == null || State != EngineState.Playing) return;
        _primary.Player.Pause();
        State = EngineState.Paused;
        StateChanged?.Invoke();
    }

    public void Resume()
    {
        if (_primary == null || State != EngineState.Paused) return;
        _primary.Player.Play();
        State = EngineState.Playing;
        StateChanged?.Invoke();
    }

    // Hard stop — disposes everything immediately with no fade.
    public void Stop()
    {
        _timer.Stop();

        if (_primary != null)
        {
            _primary.Player.PlaybackStopped -= OnPrimaryEnded;
            _primary.Dispose();
            _primary = null;
        }
        if (_secondary != null)
        {
            _secondary.Player.PlaybackStopped -= OnSecondaryEnded;
            _secondary.Dispose();
            _secondary = null;
        }

        ActiveTrackId = -1;
        State = EngineState.Stopped;
        CurrentTime = TimeSpan.Zero;
        TotalTime = TimeSpan.Zero;
        StateChanged?.Invoke();
    }

    // Seek to a position expressed as a fraction 0..1 of TotalTime.
    public void Seek(double fraction)
    {
        if (_primary == null) return;
        var total = _primary.Reader.TotalTime.TotalSeconds;
        if (total <= 0) return;
        var targetSec = Math.Clamp(fraction * total, 0, total);
        bool wasPlaying = State == EngineState.Playing;
        if (wasPlaying) _primary.Player.Pause();
        _primary.Reader.CurrentTime = TimeSpan.FromSeconds(targetSec);
        if (wasPlaying) _primary.Player.Play();
        CurrentTime = _primary.Reader.CurrentTime;
    }

    // Re-apply MasterVolume/Muted to currently active slots (call after user changes volume).
    public void ApplyVolume()
    {
        if (_primary != null) _primary.ApplySoftVolume(MasterVolume, Muted);
        if (_secondary != null) _secondary.ApplySoftVolume(MasterVolume, Muted);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        // Advance primary fade-in.
        if (_primary != null && _primary.FadeStep > 0f)
        {
            _primary.TransitionVolume = Math.Min(_primary.TransitionVolume + _primary.FadeStep, _primary.FadeTarget);
            _primary.ApplySoftVolume(MasterVolume, Muted);
            if (_primary.TransitionVolume >= _primary.FadeTarget) _primary.FadeStep = 0f;
        }

        // Advance secondary fade-out.
        if (_secondary != null && _secondary.FadeStep < 0f)
        {
            _secondary.TransitionVolume = Math.Max(_secondary.TransitionVolume + _secondary.FadeStep, 0f);
            _secondary.ApplySoftVolume(MasterVolume, Muted);
            if (_secondary.TransitionVolume <= 0f)
            {
                _secondary.Player.PlaybackStopped -= OnSecondaryEnded;
                _secondary.Dispose();
                _secondary = null;
            }
        }

        // Publish progress from the primary track.
        if (_primary != null)
        {
            CurrentTime = _primary.Reader.CurrentTime;
            TotalTime = _primary.Reader.TotalTime;
            ProgressUpdated?.Invoke();
        }
    }

    private void OnPrimaryEnded(object? sender, StoppedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_primary == null || _primary.Player != sender) return;
            _primary.Player.PlaybackStopped -= OnPrimaryEnded;
            _primary.Dispose();
            _primary = null;
            _timer.Stop();
            ActiveTrackId = -1;
            State = EngineState.Stopped;
            CurrentTime = TimeSpan.Zero;
            TotalTime = TimeSpan.Zero;
            StateChanged?.Invoke();
            TrackNaturallyEnded?.Invoke();
        });
    }

    private void OnSecondaryEnded(object? sender, StoppedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_secondary == null || _secondary.Player != sender) return;
            _secondary.Player.PlaybackStopped -= OnSecondaryEnded;
            _secondary.Dispose();
            _secondary = null;
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _primary?.Dispose();
        _secondary?.Dispose();
        _primary = null;
        _secondary = null;
    }
}
