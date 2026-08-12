using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Resona.Models;

namespace Resona.Services;

public sealed class BackgroundAnalysisService
{
    public static readonly BackgroundAnalysisService Current = new();

    private readonly object _gate = new();
    private readonly Queue<int> _pendingTrackIds = [];
    private readonly HashSet<int> _queuedTrackIds = [];
    private readonly HashSet<int> _removedActiveTrackIds = [];
    private Task? _workerTask;
    private int? _activeTrackId;
    private CancellationTokenSource? _activeAnalysisCancellation;
    private bool _pausedForTransientFailure;
    private AnalysisServerConnectionState _serverConnectionState = AnalysisServerConnectionState.NotChecked;
    private Task<bool>? _connectionCheckTask;

    public event Action<MusicTrack, string?>? TrackAnalysisFinished;
    public event Action? QueueChanged;

    public void Initialize()
    {
        EnqueueTracks(MusicLibraryService.Current.GetUnanalyzedTracks().Select(track => track.Id));
        _ = RetryServerConnectionAsync();
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
                _pendingTrackIds.Count > 0 && !HasValidServerConfiguration(),
                _serverConnectionState);
        }
    }

    public Task<bool> RetryServerConnectionAsync()
    {
        Task<bool> checkTask;
        lock (_gate)
        {
            if (_connectionCheckTask is { IsCompleted: false })
                return _connectionCheckTask;

            _serverConnectionState = AnalysisServerConnectionState.Checking;
            checkTask = CheckServerConnectionAsync();
            _connectionCheckTask = checkTask;
        }

        RaiseQueueChanged();
        return checkTask;
    }

    private async Task<bool> CheckServerConnectionAsync()
    {
        var isReachable = false;
        try
        {
            using var service = new TrackAnalysisService();
            isReachable = await service.CheckHealthAsync();
        }
        catch (Exception exception)
        {
            // The UI presents one stable offline state; details remain available on the settings page.
            WorkflowLog.Error("analysis", "Analysis server health check failed.", exception);
        }

        lock (_gate)
        {
            _serverConnectionState = isReachable
                ? AnalysisServerConnectionState.Reachable
                : AnalysisServerConnectionState.Unreachable;
            _connectionCheckTask = null;
            if (isReachable)
            {
                _pausedForTransientFailure = false;
                StartWorkerIfPossible();
            }
        }

        RaiseQueueChanged();
        return isReachable;
    }

    public void NotifyServerConfigurationChanged()
    {
        _ = RetryServerConnectionAsync();
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

    public bool RemoveTrack(int trackId)
    {
        // Persist first so this track cannot be re-added by startup recovery or
        // another enqueue request racing with the in-memory removal.
        MusicLibraryService.Current.SetTrackAnalysisDisabled(trackId, true);

        var removed = false;
        lock (_gate)
        {
            if (_pendingTrackIds.Contains(trackId))
            {
                var retainedTrackIds = _pendingTrackIds.Where(id => id != trackId).ToList();
                _pendingTrackIds.Clear();
                foreach (var retainedTrackId in retainedTrackIds)
                    _pendingTrackIds.Enqueue(retainedTrackId);
                _queuedTrackIds.Remove(trackId);
                removed = true;
            }

            if (_activeTrackId == trackId)
            {
                removed = true;
                _removedActiveTrackIds.Add(trackId);
                try { _activeAnalysisCancellation?.Cancel(); }
                catch (ObjectDisposedException) { }
            }
        }

        if (removed)
            RaiseQueueChanged();
        return removed;
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

        RaiseQueueChanged();
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

        RaiseQueueChanged();
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
                var removedByUser = false;

                try
                {
                    SetActiveTrack(trackId);
                    WorkflowLog.Info("analysis", $"Started track {trackId}.");
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
                    var shouldRetry = retryable && MusicLibraryService.Current.ShouldAnalyzeTrack(trackId);
                    lock (_gate)
                    {
                        removedByUser = _removedActiveTrackIds.Remove(trackId);
                        if (shouldRetry)
                        {
                            _pendingTrackIds.Enqueue(trackId);
                            _pausedForTransientFailure = true;
                            _serverConnectionState = AnalysisServerConnectionState.Unreachable;
                        }
                        else
                        {
                            _queuedTrackIds.Remove(trackId);
                        }
                        if (_activeTrackId == trackId)
                            _activeTrackId = null;
                        _activeAnalysisCancellation = null;
                    }
                    RaiseQueueChanged();
                }

                if (track is not null && !removedByUser)
                {
                    if (error is null)
                        WorkflowLog.Info("analysis", $"Completed track {track.Id}.");
                    else
                        WorkflowLog.Error("analysis", $"Track {track.Id} failed: {error}");
                    RaiseTrackAnalysisFinished(track, error);
                }
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
                || !HasValidServerConfiguration()
                || _serverConnectionState != AnalysisServerConnectionState.Reachable)
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
        RaiseQueueChanged();
    }

    private void StartWorkerIfPossible()
    {
        if (_workerTask is not { IsCompleted: false }
            && _pendingTrackIds.Count > 0
            && !_pausedForTransientFailure
            && HasValidServerConfiguration()
            && _serverConnectionState == AnalysisServerConnectionState.Reachable)
            _workerTask = Task.Run(ProcessQueueAsync);
    }

    private static bool HasValidServerConfiguration() =>
        TrackAnalysisService.TryNormalizeServerUrl(
            AppSettingsStore.Load().MusicAnalysisServerUrl,
            out _);

    private void RaiseQueueChanged()
    {
        if (QueueChanged is null)
            return;
        foreach (Action handler in QueueChanged.GetInvocationList())
        {
            try { handler(); }
            catch (Exception exception) { WorkflowLog.Error("analysis", "QueueChanged observer failed.", exception); }
        }
    }

    private void RaiseTrackAnalysisFinished(MusicTrack track, string? error)
    {
        if (TrackAnalysisFinished is null)
            return;
        foreach (Action<MusicTrack, string?> handler in TrackAnalysisFinished.GetInvocationList())
        {
            try { handler(track, error); }
            catch (Exception exception) { WorkflowLog.Error("analysis", "TrackAnalysisFinished observer failed.", exception); }
        }
    }
}

public sealed record BackgroundAnalysisQueueSnapshot(
    int? ActiveTrackId,
    IReadOnlyList<int> PendingTrackIds,
    bool IsWaitingForServerConfiguration,
    AnalysisServerConnectionState ServerConnectionState);

public enum AnalysisServerConnectionState
{
    NotChecked,
    Checking,
    Reachable,
    Unreachable
}
