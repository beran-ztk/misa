using System;
using System.Threading;
using System.Threading.Tasks;
using Resona.Models;

namespace Resona.Services;

public sealed record ChannelMetadataWorkStatus(
    bool IsActive,
    string OverallText,
    string CurrentText,
    int Current,
    int Total)
{
    public double Progress => Total <= 0 ? 0 : Math.Clamp((double)Current / Total, 0, 1);
    public static ChannelMetadataWorkStatus Idle { get; } = new(false, string.Empty, string.Empty, 0, 0);
}

public sealed class ChannelMetadataService
{
    private const int BackgroundBatchSize = 1;
    public static readonly ChannelMetadataService Current = new();

    private readonly object _workerGate = new();
    private Task? _workerTask;
    private int _initialized;
    private ChannelMetadataWorkStatus _status = ChannelMetadataWorkStatus.Idle;

    public event Action<int, int>? MetadataUpdated;
    public event Action? QueueChanged;
    public event Action<ChannelMetadataWorkStatus>? StatusChanged;

    public ChannelMetadataWorkStatus Status
    {
        get
        {
            lock (_workerGate)
                return _status;
        }
    }

    public void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                // Let the main window and library finish their first render
                // before touching the metadata queue.
                await Task.Delay(TimeSpan.FromSeconds(2));
                MusicLibraryService.Current.EnsureChannelMetadataQueueIndexes();
                MusicLibraryService.Current.RecoverChannelMetadataQueue();
                RequestAllChannels();
            }
            catch
            {
                // Metadata preparation is optional startup work. A later
                // channel refresh or explicit request starts it again.
            }
        });
    }

    public void RequestChannel(int channelId, int limit = 1)
    {
        MusicLibraryService.Current.QueueChannelVideoMetadata(channelId, limit);
        EnsureWorker();
        QueueChanged?.Invoke();
    }

    public void RequestVideo(int videoId)
    {
        MusicLibraryService.Current.QueueSpecificChannelVideoMetadata(videoId);
        EnsureWorker();
        QueueChanged?.Invoke();
    }

    public void RequestAutoDownloadMetadata(int limit = 1)
    {
        MusicLibraryService.Current.QueueAutoDownloadMetadata(limit);
        EnsureWorker();
        QueueChanged?.Invoke();
    }

    public void RequestAllChannels(int limit = BackgroundBatchSize)
    {
        MusicLibraryService.Current.QueueBackgroundChannelVideoMetadata(limit);
        EnsureWorker();
        QueueChanged?.Invoke();
    }

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
            var total = Math.Max(1, MusicLibraryService.Current.CountBackgroundChannelVideoMetadataWork());
            var current = 0;

            while (true)
            {
                var video = MusicLibraryService.Current.ClaimNextChannelVideoMetadata();
                if (video is null)
                {
                    var queued = MusicLibraryService.Current.QueueBackgroundChannelVideoMetadata(BackgroundBatchSize);
                    if (queued <= 0)
                        break;

                    continue;
                }

                current++;
                if (current > total)
                    total = current;
                var channelName = video.ChannelName?.Trim() ?? string.Empty;
                var title = video.Title.Trim();
                PublishStatus(new ChannelMetadataWorkStatus(
                    true,
                    $"Loading video metadata · {current:N0} of {total:N0}",
                    channelName.Length == 0 ? title : $"{channelName} · {title}",
                    current,
                    total));
                QueueChanged?.Invoke();
                YouTubeTrackMetadata? metadata = null;
                string? error = null;
                try
                {
                    metadata = await MusicLibraryService.Current.GetChannelVideoMetadataAsync(video.CanonicalUrl);
                    if (metadata is null)
                        error = "YouTube metadata unavailable";
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                }

                MusicLibraryService.Current.CompleteChannelVideoMetadata(video.Id, metadata, error);
                MetadataUpdated?.Invoke(video.ChannelId, video.Id);
                ChannelDownloadService.Current.NotifyQueueChanged();
                QueueChanged?.Invoke();
            }
        }
        finally
        {
            lock (_workerGate)
                _workerTask = null;
            PublishStatus(ChannelMetadataWorkStatus.Idle);
            if (MusicLibraryService.Current.HasQueuedChannelVideoMetadata())
                EnsureWorker();
            QueueChanged?.Invoke();
        }
    }

    private void PublishStatus(ChannelMetadataWorkStatus status)
    {
        lock (_workerGate)
            _status = status;
        StatusChanged?.Invoke(status);
    }
}
