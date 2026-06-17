using System;

namespace Music.Services;

// Tracks a single playback session for listen/skip count logic.
//
// Listen threshold: position-based — 80% of duration or within the final 30 s.
// Skip minimum:     wall-clock-based — at least 10 effective (non-paused) seconds must have
//                   elapsed before a manual interruption counts as a skip.
//
// A session is never credited for both listen and skip.
public sealed class PlaybackSession
{
    private const double ListenFraction = 0.80;
    private const double ListenFinalWindowSeconds = 30.0;
    private const double MinEffectiveSecondsForSkip = 10.0;

    private int _trackId = -1;
    private double _totalSeconds;
    private double _accumulatedWallSeconds;
    private DateTime? _lastResumeUtc;

    public bool HasSession => _trackId >= 0;
    public int TrackId => _trackId;

    // True once the listen threshold has been crossed in the current session.
    public bool ListenThresholdReached { get; private set; }

    // Start a new session. Always call before the first OnProgress.
    public void Start(int trackId, double totalSeconds)
    {
        _trackId = trackId;
        _totalSeconds = totalSeconds;
        _accumulatedWallSeconds = 0;
        _lastResumeUtc = DateTime.UtcNow;
        ListenThresholdReached = false;
    }

    // Call when playback is paused so wall-clock accumulation stops.
    public void OnPause()
    {
        if (!HasSession) return;
        Flush();
        _lastResumeUtc = null;
    }

    // Call when playback resumes after a pause.
    public void OnResume()
    {
        if (!HasSession) return;
        _lastResumeUtc = DateTime.UtcNow;
    }

    // Call on every progress tick with the current playback position in seconds.
    // Sets ListenThresholdReached when the listen position is crossed (idempotent).
    public void OnProgress(double currentSeconds)
    {
        if (!HasSession || ListenThresholdReached) return;

        Flush(); // Keep wall clock accumulation fresh.

        if (IsListenPosition(currentSeconds))
            ListenThresholdReached = true;
    }

    public void Flush()
    {
        if (_lastResumeUtc.HasValue)
        {
            _accumulatedWallSeconds += (DateTime.UtcNow - _lastResumeUtc.Value).TotalSeconds;
            _lastResumeUtc = DateTime.UtcNow;
        }
    }

    private bool IsListenPosition(double currentSeconds)
    {
        if (_totalSeconds <= 0) return false;
        if (currentSeconds >= _totalSeconds * ListenFraction) return true;
        // Final-window check only applies to tracks longer than the window itself.
        if (_totalSeconds > ListenFinalWindowSeconds && _totalSeconds - currentSeconds <= ListenFinalWindowSeconds)
            return true;
        return false;
    }

    public void Reset()
    {
        _trackId = -1;
        _totalSeconds = 0;
        _accumulatedWallSeconds = 0;
        _lastResumeUtc = null;
        ListenThresholdReached = false;
    }
}
