using System;
using System.Linq;
using System.Threading.Tasks;
using Music.Models;

namespace Music.Services;

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
        QueueChanged?.Invoke();
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
            QueueChanged?.Invoke();
        }
    }

    private async Task ProcessWorkerAsync()
    {
        while (ClaimNext() is { } video)
        {
            QueueChanged?.Invoke();
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
            DownloadFinished?.Invoke(video, track, error);
            QueueChanged?.Invoke();
        }
    }

    private ChannelVideo? ClaimNext()
    {
        lock (_claimGate)
            return MusicLibraryService.Current.ClaimNextChannelDownload();
    }
}
