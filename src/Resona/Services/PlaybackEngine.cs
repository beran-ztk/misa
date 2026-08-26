using System;
using System.Collections.Generic;
using Avalonia.Threading;
#if WINDOWS
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
#endif

namespace Resona.Services;

public enum EngineState { Stopped, Playing, Paused }

public record PlaybackSlotSnapshot(
    string FilePath,
    int TrackId,
    TimeSpan Position,
    float LoudnessGain,
    float TransitionVolume,
    float FadeTarget,
    float FadeStep);

public record PlaybackEngineSnapshot(
    EngineState State,
    PlaybackSlotSnapshot Primary,
    PlaybackSlotSnapshot? Secondary);

#if WINDOWS
// Controls playback of up to two simultaneous tracks (primary + fading-out secondary).
// Volume is controlled entirely in software via AudioFileReader.Volume (sample multiplication)
// so each slot's volume is fully independent — no shared hardware/device volume is touched.
public sealed class PlaybackEngine : IDisposable
{
    private sealed class AudioSlot : IDisposable
    {
        public readonly IWavePlayer Player;
        public readonly AudioFileReader Reader;   // owns CurrentTime, TotalTime, software Volume
        public readonly string FilePath;
        public readonly int TrackId;
        public readonly float LoudnessGain;

        // 0..1 transition factor; Reader.Volume = user volume * loudness gain * TransitionVolume.
        public float TransitionVolume;
        public float FadeTarget;   // 0 = fade out fully, 1 = fade in to full
        public float FadeStep;     // per 100 ms tick, signed; 0 = steady

        public AudioSlot(
            IWavePlayer player,
            AudioFileReader reader,
            string filePath,
            int trackId,
            float loudnessGain,
            float startTransition)
        {
            Player = player;
            Reader = reader;
            FilePath = filePath;
            TrackId = trackId;
            LoudnessGain = loudnessGain;
            TransitionVolume = startTransition;
            FadeTarget = startTransition;
        }

        // Set the effective output level: user volume * per-track loudness gain * transition fade.
        public void ApplySoftVolume(float masterVolume) =>
            Reader.Volume = Math.Clamp(masterVolume * LoudnessGain * TransitionVolume, 0f, 1.15f);

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

    private float _masterVolume = 1f;

    private AudioSlot? _primary;
    private AudioSlot? _secondary;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private bool _disposed;

    public PlaybackEngine() => _timer.Tick += OnTick;

    // Begin playing filePath.
    // If a track is already playing it fades out over fadeOutSeconds (0 = immediate stop).
    // The new track fades in over fadeInSeconds (0 = starts at full user volume immediately).
    public void Play(string filePath, int trackId, float fadeOutSeconds, float fadeInSeconds, float loudnessGain = 1f)
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
        _primary = new AudioSlot(player, reader, filePath, trackId, loudnessGain, startTransition)
        {
            FadeTarget = 1f,
            FadeStep = fadeInSeconds > 0f ? 1f / (fadeInSeconds * 10f) : 0f,
        };
        _primary.ApplySoftVolume(_masterVolume);

        player.Play();
        ActiveTrackId = trackId;
        CurrentTime = reader.CurrentTime;
        TotalTime = reader.TotalTime;
        State = EngineState.Playing;
        _timer.Start();
        StateChanged?.Invoke();
    }

    public void Pause()
    {
        if (_primary == null || State != EngineState.Playing) return;
        _primary.Player.Pause();
        _secondary?.Player.Pause();
        _timer.Stop();
        State = EngineState.Paused;
        StateChanged?.Invoke();
    }

    public void Resume()
    {
        if (_primary == null || State != EngineState.Paused) return;
        _primary.Player.Play();
        _secondary?.Player.Play();
        _timer.Start();
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

    /// <summary>Captures both audible slots so a temporary preview can restore an in-progress crossfade exactly.</summary>
    public PlaybackEngineSnapshot? CaptureSnapshot()
    {
        if (_primary is null || State == EngineState.Stopped) return null;
        return new PlaybackEngineSnapshot(State, CaptureSlot(_primary), _secondary is null ? null : CaptureSlot(_secondary));
    }

    public void RestoreSnapshot(PlaybackEngineSnapshot? snapshot)
    {
        Stop();
        if (snapshot is null || _disposed) return;

        _secondary = snapshot.Secondary is null ? null : CreateSlot(snapshot.Secondary, isPrimary: false);
        _primary = CreateSlot(snapshot.Primary, isPrimary: true);
        ActiveTrackId = snapshot.Primary.TrackId;
        CurrentTime = _primary.Reader.CurrentTime;
        TotalTime = _primary.Reader.TotalTime;
        State = snapshot.State;

        if (State == EngineState.Playing)
        {
            _secondary?.Player.Play();
            _primary.Player.Play();
            _timer.Start();
        }

        StateChanged?.Invoke();
        ProgressUpdated?.Invoke();
    }

    private static PlaybackSlotSnapshot CaptureSlot(AudioSlot slot) => new(
        slot.FilePath, slot.TrackId, slot.Reader.CurrentTime, slot.LoudnessGain, slot.TransitionVolume, slot.FadeTarget, slot.FadeStep);

    private AudioSlot CreateSlot(PlaybackSlotSnapshot snapshot, bool isPrimary)
    {
        var reader = new AudioFileReader(snapshot.FilePath);
        reader.CurrentTime = snapshot.Position <= reader.TotalTime ? snapshot.Position : reader.TotalTime;
        var player = new WaveOutEvent();
        if (isPrimary) player.PlaybackStopped += OnPrimaryEnded;
        else player.PlaybackStopped += OnSecondaryEnded;
        player.Init(reader);
        var slot = new AudioSlot(player, reader, snapshot.FilePath, snapshot.TrackId, snapshot.LoudnessGain, snapshot.TransitionVolume)
        {
            FadeTarget = snapshot.FadeTarget,
            FadeStep = snapshot.FadeStep
        };
        slot.ApplySoftVolume(_masterVolume);
        return slot;
    }

    // Seek to a position expressed as a fraction 0..1 of TotalTime.
    public void Seek(double fraction)
    {
        if (_primary == null) return;
        var total = _primary.Reader.TotalTime.TotalSeconds;
        if (total <= 0) return;
        var targetSec = Math.Clamp(fraction * total, 0, total);
        bool wasPlaying = State == EngineState.Playing;
        if (wasPlaying)
        {
            _primary.Player.Pause();
            _secondary?.Player.Pause();
        }
        _primary.Reader.CurrentTime = TimeSpan.FromSeconds(targetSec);
        if (wasPlaying)
        {
            _primary.Player.Play();
            _secondary?.Player.Play();
        }
        CurrentTime = _primary.Reader.CurrentTime;
    }

    // Re-apply the user volume to all active slots without changing their fade position.
    public void ApplyVolume(float masterVolume)
    {
        _masterVolume = Math.Clamp(masterVolume, 0f, 1f);
        _primary?.ApplySoftVolume(_masterVolume);
        _secondary?.ApplySoftVolume(_masterVolume);
    }

    public static float CalculateLoudnessGain(double? integratedLoudness, double? loudnessRange)
    {
        if (integratedLoudness is not double lufs || double.IsNaN(lufs) || double.IsInfinity(lufs))
            return 1f;

        const double targetLufs = -14.0;
        var gainDb = targetLufs - lufs;

        if (gainDb > 0 && loudnessRange is double range && !double.IsNaN(range) && !double.IsInfinity(range))
        {
            if (range >= 16)
                gainDb *= 0.55;
            else if (range >= 10)
                gainDb *= 0.75;
        }

        gainDb = Math.Clamp(gainDb, -10.0, 6.0);
        return (float)Math.Pow(10.0, gainDb / 20.0);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        // Advance primary fade-in.
        if (_primary != null && _primary.FadeStep > 0f)
        {
            _primary.TransitionVolume = Math.Min(_primary.TransitionVolume + _primary.FadeStep, _primary.FadeTarget);
            _primary.ApplySoftVolume(_masterVolume);
            if (_primary.TransitionVolume >= _primary.FadeTarget) _primary.FadeStep = 0f;
        }

        // Advance secondary fade-out.
        if (_secondary != null && _secondary.FadeStep < 0f)
        {
            _secondary.TransitionVolume = Math.Max(_secondary.TransitionVolume + _secondary.FadeStep, 0f);
            _secondary.ApplySoftVolume(_masterVolume);
            if (_secondary.TransitionVolume <= 0f)
            {
                _secondary.Player.PlaybackStopped -= OnSecondaryEnded;
                _secondary.Dispose();
                _secondary = null;
            }
        }

        // Publish progress from the primary track.
        var primary = _primary;
        if (primary != null)
        {
            CurrentTime = primary.Reader.CurrentTime;
            TotalTime = primary.Reader.TotalTime;
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
#endif
