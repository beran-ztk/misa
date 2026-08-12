using System;
using System.Linq;
using System.Threading.Tasks;
using Resona.Models;

namespace Resona.Services;

public sealed class ChannelDownloadService
{
    public static readonly ChannelDownloadService Current = new();

    private readonly object _workerGate = new();
    private readonly object _claimGate = new();
    private Task? _workerTask;

    public event Action<ChannelVideo, MusicTrack?, string?>? DownloadFinished;
    public event Action? QueueChanged;

    public void Initialize()
    {
        MusicLibraryService.Current.RecoverChannelDownloads();
        EnsureWorker();
    }

    public void NotifyQueueChanged()
    {
        EnsureWorker();
        RaiseQueueChanged();
    }

    public ChannelDownloadSummary GetSummary() => MusicLibraryService.Current.GetChannelDownloadSummary();

    private void EnsureWorker()
    {
        lock (_workerGate)
        {
            if (_workerTask is { IsCompleted: false })
                return;
            _workerTask = Task.Run(ProcessQueueAsync);
        }
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            var workers = Enumerable.Range(0, Math.Max(1, Values.MaxParallelDownloadWorkers))
                .Select(_ => ProcessWorkerAsync())
                .ToArray();
            await Task.WhenAll(workers);
        }
        finally
        {
            lock (_workerGate)
            {
                _workerTask = null;
                if (MusicLibraryService.Current.GetChannelDownloadSummary().Queued > 0)
                    _workerTask = Task.Run(ProcessQueueAsync);
            }
            RaiseQueueChanged();
        }
    }

    private async Task ProcessWorkerAsync()
    {
        while (ClaimNext() is { } video)
        {
            WorkflowLog.Info("channel-download", $"Claimed video {video.Id}, attempt {video.DownloadAttempts}.");
            RaiseQueueChanged();
            MusicTrack? track = null;
            string? error = null;
            try
            {
                var result = await MusicLibraryService.Current.PreloadChannelVideoAsync(video);
                track = result.Track;
                error = result.Error;
            }
            catch (Exception exception)
            {
                error = exception.Message;
            }

            MusicLibraryService.Current.CompleteChannelDownload(video.Id, track is not null, error);
            var shouldRetry = track is null && video.DownloadAttempts < 3;
            if (track is null)
            {
                WorkflowLog.Error("channel-download", $"Video {video.Id} failed on attempt {video.DownloadAttempts}: {error}");
            }
            else
            {
                WorkflowLog.Info("channel-download", $"Video {video.Id} completed as track {track.Id}.");
            }

            RaiseDownloadFinished(video, track, error);
            RaiseQueueChanged();
            if (shouldRetry)
                await Task.Delay(RetryDelay(video.DownloadAttempts));
        }
    }

    private static TimeSpan RetryDelay(int attempt) =>
        TimeSpan.FromSeconds(Math.Min(8, Math.Pow(2, Math.Max(0, attempt - 1))));

    private void RaiseDownloadFinished(ChannelVideo video, MusicTrack? track, string? error)
    {
        if (DownloadFinished is null)
            return;
        foreach (Action<ChannelVideo, MusicTrack?, string?> handler in DownloadFinished.GetInvocationList())
        {
            try { handler(video, track, error); }
            catch (Exception exception) { WorkflowLog.Error("channel-download", "DownloadFinished observer failed.", exception); }
        }
    }

    private void RaiseQueueChanged()
    {
        if (QueueChanged is null)
            return;
        foreach (Action handler in QueueChanged.GetInvocationList())
        {
            try { handler(); }
            catch (Exception exception) { WorkflowLog.Error("channel-download", "QueueChanged observer failed.", exception); }
        }
    }

    private ChannelVideo? ClaimNext()
    {
        lock (_claimGate)
            return MusicLibraryService.Current.ClaimNextChannelDownload();
    }
}
