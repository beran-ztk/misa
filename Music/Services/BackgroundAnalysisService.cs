using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        lock (_gate)
        {
            return new BackgroundAnalysisQueueSnapshot(
                _activeTrackId,
                _pendingTrackIds.ToList());
        }
    }

    private void EnqueueTracks(IEnumerable<int> trackIds)
    {
        lock (_gate)
        {
            foreach (var trackId in trackIds)
            {
                if (!_queuedTrackIds.Add(trackId))
                    continue;

                _pendingTrackIds.Enqueue(trackId);
            }

            if (_workerTask is not { IsCompleted: false } && _pendingTrackIds.Count > 0)
                _workerTask = Task.Run(ProcessQueueAsync);
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

                try
                {
                    SetActiveTrack(trackId);
                    track = MusicLibraryService.Current.GetTrackById(trackId);
                    if (track is null || MusicLibraryService.Current.GetTrackAudioAnalysis(track.Id) is not null)
                        continue;

                    error = await MusicLibraryService.Current.AnalyzeTrackAsync(track);
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                }
                finally
                {
                    lock (_gate)
                    {
                        _queuedTrackIds.Remove(trackId);
                        if (_activeTrackId == trackId)
                            _activeTrackId = null;
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
                _workerTask = _pendingTrackIds.Count > 0
                    ? Task.Run(ProcessQueueAsync)
                    : null;
            }
        }
    }

    private bool TryDequeue(out int trackId)
    {
        lock (_gate)
        {
            if (_pendingTrackIds.Count == 0)
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
}

public sealed record BackgroundAnalysisQueueSnapshot(int? ActiveTrackId, IReadOnlyList<int> PendingTrackIds);
