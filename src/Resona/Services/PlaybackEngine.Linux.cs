#if !WINDOWS
using System;
using Avalonia.Threading;
using LibVLCSharp.Shared;

namespace Resona.Services;

// Linux playback implementation backed by two independent libVLC media players.
// This preserves the Windows engine's crossfade, seeking, snapshot and loudness behavior.
public sealed class PlaybackEngine : IDisposable
{
    private sealed class AudioSlot : IDisposable
    {
        public readonly Media Media;
        public readonly MediaPlayer Player;
        public readonly string FilePath;
        public readonly int TrackId;
        public readonly float LoudnessGain;

        public float TransitionVolume;
        public float FadeTarget;
        public float FadeStep;

        public AudioSlot(
            LibVLC libVlc,
            string filePath,
            int trackId,
            float loudnessGain,
            float transitionVolume)
        {
            FilePath = filePath;
            TrackId = trackId;
            LoudnessGain = loudnessGain;
            TransitionVolume = transitionVolume;
            FadeTarget = transitionVolume;
            Media = new Media(libVlc, filePath, FromType.FromPath);
            Player = new MediaPlayer(libVlc);
            Player.Media = Media;
        }

        public void ApplyVolume(float masterVolume)
        {
            var effectiveVolume = Math.Clamp(
                masterVolume * LoudnessGain * TransitionVolume,
                0f,
                1.15f);
            Player.Volume = (int)Math.Round(effectiveVolume * 100);
        }

        public void Dispose()
        {
            try { Player.Stop(); } catch { }
            try { Player.Dispose(); } catch { }
            try { Media.Dispose(); } catch { }
        }
    }

    public event Action? TrackNaturallyEnded;
    public event Action? StateChanged;
    public event Action? ProgressUpdated;

    public EngineState State { get; private set; } = EngineState.Stopped;
    public int ActiveTrackId { get; private set; } = -1;
    public TimeSpan CurrentTime { get; private set; }
    public TimeSpan TotalTime { get; private set; }
    public bool IsCrossfading => _secondary is not null;

    private LibVLC? _libVlc;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private AudioSlot? _primary;
    private AudioSlot? _secondary;
    private float _masterVolume = 1f;
    private bool _disposed;

    public PlaybackEngine()
    {
        _timer.Tick += OnTick;
    }

    public void Play(
        string filePath,
        int trackId,
        float fadeOutSeconds,
        float fadeInSeconds,
        float loudnessGain = 1f)
    {
        if (_disposed)
            return;

        EnsureLibVlc();
        DisposeSecondary();

        if (_primary is not null)
        {
            _primary.Player.EndReached -= OnPrimaryEnded;
            _primary.Player.EndReached += OnSecondaryEnded;
            _secondary = _primary;
            _primary = null;

            if (fadeOutSeconds > 0f && _secondary.TransitionVolume > 0f)
            {
                _secondary.FadeTarget = 0f;
                _secondary.FadeStep = -_secondary.TransitionVolume / (fadeOutSeconds * 10f);
            }
            else
            {
                DisposeSecondary();
            }
        }

        var startTransition = fadeInSeconds > 0f ? 0f : 1f;
        _primary = CreateSlot(
            filePath,
            trackId,
            loudnessGain,
            startTransition,
            fadeTarget: 1f,
            fadeStep: fadeInSeconds > 0f ? 1f / (fadeInSeconds * 10f) : 0f,
            isPrimary: true);
        _primary.ApplyVolume(_masterVolume);

        if (!_primary.Player.Play())
        {
            DisposePrimary();
            throw new InvalidOperationException($"libVLC could not play '{filePath}'.");
        }

        ActiveTrackId = trackId;
        CurrentTime = TimeSpan.Zero;
        TotalTime = TimeSpan.Zero;
        State = EngineState.Playing;
        _timer.Start();
        StateChanged?.Invoke();
    }

    public void Pause()
    {
        if (_primary is null || State != EngineState.Playing)
            return;

        _primary.Player.SetPause(true);
        _secondary?.Player.SetPause(true);
        _timer.Stop();
        State = EngineState.Paused;
        StateChanged?.Invoke();
    }

    public void Resume()
    {
        if (_primary is null || State != EngineState.Paused)
            return;

        _primary.Player.SetPause(false);
        _secondary?.Player.SetPause(false);
        _timer.Start();
        State = EngineState.Playing;
        StateChanged?.Invoke();
    }

    public void Stop()
    {
        _timer.Stop();
        DisposePrimary();
        DisposeSecondary();
        ActiveTrackId = -1;
        State = EngineState.Stopped;
        CurrentTime = TimeSpan.Zero;
        TotalTime = TimeSpan.Zero;
        StateChanged?.Invoke();
    }

    public PlaybackEngineSnapshot? CaptureSnapshot()
    {
        if (_primary is null || State == EngineState.Stopped)
            return null;

        return new PlaybackEngineSnapshot(
            State,
            CaptureSlot(_primary),
            _secondary is null ? null : CaptureSlot(_secondary));
    }

    public void RestoreSnapshot(PlaybackEngineSnapshot? snapshot)
    {
        Stop();
        if (snapshot is null || _disposed)
            return;

        EnsureLibVlc();
        _secondary = snapshot.Secondary is null
            ? null
            : RestoreSlot(snapshot.Secondary, isPrimary: false);
        _primary = RestoreSlot(snapshot.Primary, isPrimary: true);

        ActiveTrackId = snapshot.Primary.TrackId;
        CurrentTime = snapshot.Primary.Position;
        TotalTime = PlayerDuration(_primary.Player);
        State = snapshot.State;

        if (State == EngineState.Playing)
        {
            _secondary?.Player.SetPause(false);
            _primary.Player.SetPause(false);
            _timer.Start();
        }
        else
        {
            _secondary?.Player.SetPause(true);
            _primary.Player.SetPause(true);
        }

        StateChanged?.Invoke();
        ProgressUpdated?.Invoke();
    }

    public void Seek(double fraction)
    {
        if (_primary is null)
            return;

        fraction = Math.Clamp(fraction, 0, 1);
        _primary.Player.Position = (float)fraction;
        CurrentTime = PlayerPosition(_primary.Player);
        TotalTime = PlayerDuration(_primary.Player);
        ProgressUpdated?.Invoke();
    }

    public void ApplyVolume(float masterVolume)
    {
        _masterVolume = Math.Clamp(masterVolume, 0f, 1f);
        _primary?.ApplyVolume(_masterVolume);
        _secondary?.ApplyVolume(_masterVolume);
    }

    public static float CalculateLoudnessGain(double? integratedLoudness, double? loudnessRange)
        => LoudnessNormalizer.CalculateGain(integratedLoudness, loudnessRange);

    private AudioSlot CreateSlot(
        string filePath,
        int trackId,
        float loudnessGain,
        float transitionVolume,
        float fadeTarget,
        float fadeStep,
        bool isPrimary)
    {
        var slot = new AudioSlot(
            _libVlc ?? throw new InvalidOperationException("libVLC is not initialized."),
            filePath,
            trackId,
            loudnessGain,
            transitionVolume)
        {
            FadeTarget = fadeTarget,
            FadeStep = fadeStep
        };
        if (isPrimary)
            slot.Player.EndReached += OnPrimaryEnded;
        else
            slot.Player.EndReached += OnSecondaryEnded;
        slot.ApplyVolume(_masterVolume);
        return slot;
    }

    private AudioSlot RestoreSlot(PlaybackSlotSnapshot snapshot, bool isPrimary)
    {
        var slot = CreateSlot(
            snapshot.FilePath,
            snapshot.TrackId,
            snapshot.LoudnessGain,
            snapshot.TransitionVolume,
            snapshot.FadeTarget,
            snapshot.FadeStep,
            isPrimary);
        if (!slot.Player.Play())
        {
            slot.Dispose();
            throw new InvalidOperationException($"libVLC could not restore '{snapshot.FilePath}'.");
        }
        slot.Player.Time = Math.Max(0, (long)snapshot.Position.TotalMilliseconds);
        return slot;
    }

    private static PlaybackSlotSnapshot CaptureSlot(AudioSlot slot) => new(
        slot.FilePath,
        slot.TrackId,
        PlayerPosition(slot.Player),
        slot.LoudnessGain,
        slot.TransitionVolume,
        slot.FadeTarget,
        slot.FadeStep);

    private void OnTick(object? sender, EventArgs e)
    {
        if (_primary is not null && _primary.FadeStep > 0f)
        {
            _primary.TransitionVolume = Math.Min(
                _primary.TransitionVolume + _primary.FadeStep,
                _primary.FadeTarget);
            _primary.ApplyVolume(_masterVolume);
            if (_primary.TransitionVolume >= _primary.FadeTarget)
                _primary.FadeStep = 0f;
        }

        if (_secondary is not null && _secondary.FadeStep < 0f)
        {
            _secondary.TransitionVolume = Math.Max(
                _secondary.TransitionVolume + _secondary.FadeStep,
                0f);
            _secondary.ApplyVolume(_masterVolume);
            if (_secondary.TransitionVolume <= 0f)
                DisposeSecondary();
        }

        if (_primary is null)
            return;

        CurrentTime = PlayerPosition(_primary.Player);
        TotalTime = PlayerDuration(_primary.Player);
        ProgressUpdated?.Invoke();

    }

    private void OnPrimaryEnded(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_primary is null || !ReferenceEquals(_primary.Player, sender))
                return;

            DisposePrimary();
            _timer.Stop();
            ActiveTrackId = -1;
            State = EngineState.Stopped;
            CurrentTime = TimeSpan.Zero;
            TotalTime = TimeSpan.Zero;
            StateChanged?.Invoke();
            TrackNaturallyEnded?.Invoke();
        });
    }

    private void OnSecondaryEnded(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_secondary is null || !ReferenceEquals(_secondary.Player, sender))
                return;
            DisposeSecondary();
        });
    }

    private void DisposePrimary()
    {
        if (_primary is null)
            return;
        _primary.Player.EndReached -= OnPrimaryEnded;
        _primary.Dispose();
        _primary = null;
    }

    private void DisposeSecondary()
    {
        if (_secondary is null)
            return;
        _secondary.Player.EndReached -= OnSecondaryEnded;
        _secondary.Dispose();
        _secondary = null;
    }

    private static TimeSpan PlayerPosition(MediaPlayer player) =>
        TimeSpan.FromMilliseconds(Math.Max(0, player.Time));

    private static TimeSpan PlayerDuration(MediaPlayer player) =>
        TimeSpan.FromMilliseconds(Math.Max(0, player.Length));

    private void EnsureLibVlc()
    {
        if (_libVlc is not null)
            return;

        try
        {
            LibVLCSharp.Shared.Core.Initialize();
            _libVlc = new LibVLC("--no-video", "--quiet");
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "libVLC is unavailable. Install VLC/libvlc for your Linux distribution and restart Resona.",
                exception);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _timer.Stop();
        DisposePrimary();
        DisposeSecondary();
        _libVlc?.Dispose();
        _libVlc = null;
    }
}
#endif
