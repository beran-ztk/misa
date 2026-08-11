using System;
using System.Threading.Tasks;
using Resona.Models;

namespace Resona.Services;

public sealed class ChannelMetadataService
{
    private const int BackgroundBatchSize = 32;
    public static readonly ChannelMetadataService Current = new();

    private readonly object _workerGate = new();
    private Task? _workerTask;

    public event Action<int, int>? MetadataUpdated;
    public event Action? QueueChanged;

    public void Initialize()
    {
        MusicLibraryService.Current.RecoverChannelMetadataQueue();
        RequestFollowedChannels();
    }

    public void RequestChannel(int channelId, int limit = 20)
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

    public void RequestAutoDownloadMetadata(int limit = 40)
    {
        MusicLibraryService.Current.QueueAutoDownloadMetadata(limit);
        EnsureWorker();
        QueueChanged?.Invoke();
    }

    public void RequestFollowedChannels(int limit = BackgroundBatchSize)
    {
        MusicLibraryService.Current.QueueFollowedChannelVideoMetadata(limit);
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
            while (MusicLibraryService.Current.ClaimNextChannelVideoMetadata() is { } video)
            {
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
            // Keep a small queue instead of scheduling thousands of rows at
            // once. When one batch is exhausted, enqueue the next pending
            // metadata entries from every followed channel.
            MusicLibraryService.Current.QueueFollowedChannelVideoMetadata(BackgroundBatchSize);
            if (MusicLibraryService.Current.HasQueuedChannelVideoMetadata())
                EnsureWorker();
            QueueChanged?.Invoke();
        }
    }
}
