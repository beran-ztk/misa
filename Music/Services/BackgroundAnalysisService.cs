using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Music.Models;

namespace Music.Services;

public sealed class BackgroundAnalysisService
{
    public static readonly BackgroundAnalysisService Current = new();

    private readonly object _gate = new();
    private readonly Queue<int> _pendingTrackIds = [];
    private readonly HashSet<int> _queuedTrackIds = [];
    private Task? _workerTask;
    private int? _activeTrackId;
    private CancellationTokenSource? _activeAnalysisCancellation;
    private bool _pausedForTransientFailure;

    public event Action<MusicTrack, string?>? TrackAnalysisFinished;
    public event Action? QueueChanged;

    public void Initialize()
    {
        EnqueueTracks(MusicLibraryService.Current.GetUnanalyzedTracks().Select(track => track.Id));
    }

    public void EnqueueTrack(int trackId)
    {
        EnqueueTracks([trackId]);
    }

    public BackgroundAnalysisQueueSnapshot GetSnapshot()
    {
        PruneIneligibleTracks();
        lock (_gate)
        {
            return new BackgroundAnalysisQueueSnapshot(
                _activeTrackId,
                _pendingTrackIds.ToList(),
                _pendingTrackIds.Count > 0 && !HasValidServerConfiguration());
        }
    }

    public void NotifyServerConfigurationChanged()
    {
        lock (_gate)
        {
            _pausedForTransientFailure = false;
            StartWorkerIfPossible();
        }
        QueueChanged?.Invoke();
    }

    public bool CancelActiveAnalysis()
    {
        lock (_gate)
        {
            if (_activeAnalysisCancellation is null)
                return false;
            try
            {
                _activeAnalysisCancellation.Cancel();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }
    }

    private void EnqueueTracks(IEnumerable<int> trackIds)
    {
        var eligibleTrackIds = trackIds
            .Where(MusicLibraryService.Current.ShouldAnalyzeTrack)
            .ToList();

        lock (_gate)
        {
            foreach (var trackId in eligibleTrackIds)
            {
                if (!_queuedTrackIds.Add(trackId))
                    continue;

                _pendingTrackIds.Enqueue(trackId);
            }

            StartWorkerIfPossible();
        }

        QueueChanged?.Invoke();
    }

    private void PruneIneligibleTracks()
    {
        List<int> pendingTrackIds;
        lock (_gate)
            pendingTrackIds = _pendingTrackIds.ToList();

        if (pendingTrackIds.Count == 0)
            return;

        var eligibleTrackIds = pendingTrackIds
            .Where(MusicLibraryService.Current.ShouldAnalyzeTrack)
            .ToHashSet();
        if (eligibleTrackIds.Count == pendingTrackIds.Count)
            return;

        lock (_gate)
        {
            var retainedTrackIds = _pendingTrackIds
                .Where(eligibleTrackIds.Contains)
                .ToList();

            _pendingTrackIds.Clear();
            _queuedTrackIds.Clear();
            foreach (var trackId in retainedTrackIds)
            {
                _pendingTrackIds.Enqueue(trackId);
                _queuedTrackIds.Add(trackId);
            }

            if (_activeTrackId is int activeTrackId)
                _queuedTrackIds.Add(activeTrackId);
        }

        QueueChanged?.Invoke();
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            while (TryDequeue(out var trackId))
            {
                MusicTrack? track = null;
                string? error = null;
                var retryable = false;

                try
                {
                    SetActiveTrack(trackId);
                    using var cancellation = new CancellationTokenSource();
                    lock (_gate)
                        _activeAnalysisCancellation = cancellation;
                    track = MusicLibraryService.Current.GetTrackById(trackId);
                    if (track is null
                        || track.AnalysisDisabled
                        || MusicLibraryService.Current.GetTrackAudioAnalysis(track.Id) is not null)
                        continue;

                    var outcome = await MusicLibraryService.Current.AnalyzeTrackAsync(track, cancellation.Token);
                    error = outcome.Error;
                    retryable = outcome.Retryable;
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                }
                finally
                {
                    lock (_gate)
                    {
                        if (retryable)
                        {
                            _pendingTrackIds.Enqueue(trackId);
                            _pausedForTransientFailure = true;
                        }
                        else
                        {
                            _queuedTrackIds.Remove(trackId);
                        }
                        if (_activeTrackId == trackId)
                            _activeTrackId = null;
                        _activeAnalysisCancellation = null;
                    }
                    QueueChanged?.Invoke();
                }

                if (track is not null)
                    TrackAnalysisFinished?.Invoke(track, error);
            }
        }
        finally
        {
            lock (_gate)
            {
                _workerTask = null;
                StartWorkerIfPossible();
            }
        }
    }

    private bool TryDequeue(out int trackId)
    {
        lock (_gate)
        {
            if (_pendingTrackIds.Count == 0
                || _pausedForTransientFailure
                || !HasValidServerConfiguration())
            {
                trackId = -1;
                return false;
            }

            trackId = _pendingTrackIds.Dequeue();
            return true;
        }
    }

    private void SetActiveTrack(int trackId)
    {
        lock (_gate)
            _activeTrackId = trackId;
        QueueChanged?.Invoke();
    }

    private void StartWorkerIfPossible()
    {
        if (_workerTask is not { IsCompleted: false }
            && _pendingTrackIds.Count > 0
            && !_pausedForTransientFailure
            && HasValidServerConfiguration())
            _workerTask = Task.Run(ProcessQueueAsync);
    }

    private static bool HasValidServerConfiguration() =>
        TrackAnalysisService.TryNormalizeServerUrl(
            AppSettingsStore.Load().MusicAnalysisServerUrl,
            out _);
}

public sealed record BackgroundAnalysisQueueSnapshot(
    int? ActiveTrackId,
    IReadOnlyList<int> PendingTrackIds,
    bool IsWaitingForServerConfiguration);
