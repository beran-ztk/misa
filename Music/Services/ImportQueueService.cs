using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Music.Models;

namespace Music.Services;

/// <summary>Persists import plans and processes one item at a time without blocking the UI.</summary>
public sealed class ImportQueueService
{
    public static readonly ImportQueueService Current = new();

    private readonly TrackDownloadService _downloader = new();
    private readonly object _workerGate = new();
    private Task? _workerTask;

    public event Action<ImportQueueItem>? ItemUpdated;
    public event Action<MusicTrack, string?>? TrackImported;

    public void Initialize()
    {
        MusicLibraryService.Current.RequeueInterruptedImports();
        EnsureWorker();
    }

    public async Task<ImportPreview> PreviewAsync(IEnumerable<string> sourceUrls, IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var previewItems = new List<ImportPreviewItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var existingCount = 0;
        var duplicateCount = 0;
        var unavailableCount = 0;
        var alreadyQueued = MusicLibraryService.Current.GetActiveImportCanonicalUrls();

        foreach (var sourceUrl in sourceUrls.Select(url => url.Trim()).Where(url => url.Length > 0))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report("Reading link…");
            using var sourceTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            sourceTimeout.CancelAfter(TimeSpan.FromSeconds(45));
            IReadOnlyList<YouTubePlaylistEntry> entries;
            try { entries = await _downloader.GetPlaylistEntriesAsync(sourceUrl, sourceTimeout.Token); }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                unavailableCount++;
                previewItems.Add(new ImportPreviewItem(sourceUrl, sourceUrl, "This link took too long to read", null, null,
                    ImportQueueStatus.Failed, "Timed out after 45 seconds"));
                continue;
            }
            if (entries.Count == 0)
            {
                unavailableCount++;
                previewItems.Add(new ImportPreviewItem(sourceUrl, sourceUrl, "Could not read this link", null, null,
                    ImportQueueStatus.Failed, "Unavailable or unsupported playlist"));
                continue;
            }

            foreach (var entry in entries)
            {
                if (!seen.Add(entry.CanonicalUrl))
                {
                    duplicateCount++;
                    previewItems.Add(new ImportPreviewItem(entry.SourceUrl, entry.CanonicalUrl, entry.Title, entry.DurationSeconds,
                        null, ImportQueueStatus.Skipped, "Duplicate in this import"));
                    continue;
                }
                if (MusicLibraryService.Current.TrackExistsByCanonicalUrl(entry.CanonicalUrl))
                {
                    existingCount++;
                    previewItems.Add(new ImportPreviewItem(entry.SourceUrl, entry.CanonicalUrl, entry.Title, entry.DurationSeconds,
                        null, ImportQueueStatus.Skipped, "Already in library"));
                    continue;
                }
                if (alreadyQueued.Contains(entry.CanonicalUrl))
                {
                    duplicateCount++;
                    previewItems.Add(new ImportPreviewItem(entry.SourceUrl, entry.CanonicalUrl, entry.Title, entry.DurationSeconds,
                        null, ImportQueueStatus.Skipped, "Already in the import queue"));
                    continue;
                }

                progress?.Report("Reading track details…");
                YouTubeTrackMetadata? metadata = null;
                try
                {
                    using var metadataTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    metadataTimeout.CancelAfter(TimeSpan.FromSeconds(20));
                    metadata = await _downloader.GetMetadataAsync(entry.CanonicalUrl, metadataTimeout.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // The flat playlist already gave us a valid queue item. Size/estimate may simply remain unknown.
                }
                var duration = metadata?.DurationSeconds ?? entry.DurationSeconds;
                previewItems.Add(new ImportPreviewItem(entry.SourceUrl, entry.CanonicalUrl,
                    metadata?.Title ?? entry.Title, duration, metadata?.EstimatedAudioSizeBytes, ImportQueueStatus.Queued));
            }
        }

        var queued = previewItems.Where(item => item.Status == ImportQueueStatus.Queued).ToList();
        var sizes = queued.Where(item => item.EstimatedSizeBytes is not null).Select(item => item.EstimatedSizeBytes!.Value).ToList();
        var downloadTimes = queued.Select(item => MusicLibraryService.Current.EstimateDownloadDuration(item.DurationSeconds, item.EstimatedSizeBytes))
            .Where(time => time is not null).Select(time => time!.Value).ToList();
        var analysisTimes = queued.Select(item => MusicLibraryService.Current.EstimateAnalysisDuration(item.DurationSeconds, item.EstimatedSizeBytes))
            .Where(time => time is not null).Select(time => time!.Value).ToList();

        return new ImportPreview(previewItems, existingCount, duplicateCount, unavailableCount,
            sizes.Count == queued.Count && sizes.Count > 0 ? sizes.Sum() : null,
            downloadTimes.Count == queued.Count && downloadTimes.Count > 0 ? TimeSpan.FromTicks(downloadTimes.Sum(time => time.Ticks)) : null,
            analysisTimes.Count == queued.Count && analysisTimes.Count > 0 ? TimeSpan.FromTicks(analysisTimes.Sum(time => time.Ticks)) : null);
    }

    public void Queue(ImportPreview preview)
    {
        var queued = preview.Items.Where(item => item.Status == ImportQueueStatus.Queued).ToList();
        if (queued.Count == 0) return;
        MusicLibraryService.Current.CreateImportBatch(queued[0].SourceUrl, queued);
        EnsureWorker();
    }

    public ImportQueueSummary GetSummary() => MusicLibraryService.Current.GetImportQueueSummary();
    public IReadOnlyList<ImportQueueSource> GetSources() => MusicLibraryService.Current.GetImportQueueSources();
    public bool RemoveQueuedItem(int id) => MusicLibraryService.Current.RemoveQueuedImport(id);

    private void EnsureWorker()
    {
        lock (_workerGate)
        {
            if (_workerTask is { IsCompleted: false }) return;
            _workerTask = Task.Run(ProcessQueueAsync);
        }
    }

    private async Task ProcessQueueAsync()
    {
        while (MusicLibraryService.Current.GetNextQueuedImport() is { } item)
        {
            Update(item, ImportQueueStatus.Downloading, "Checking download details…");
            var result = await MusicLibraryService.Current.ImportFromYouTubeAsync(item.CanonicalUrl,
                new Progress<string>(message =>
                {
                    var status = message.StartsWith("Analyzing", StringComparison.OrdinalIgnoreCase)
                        ? ImportQueueStatus.Analyzing
                        : ImportQueueStatus.Downloading;
                    Update(item, status, message);
                }));

            if (result.Success && result.Track is not null)
            {
                MusicLibraryService.Current.SetTrackNeedsReview(result.Track.Id, true);
                Update(item, ImportQueueStatus.ReadyForReview, result.Warning ?? "Ready for review", result.Track.Id);
                TrackImported?.Invoke(result.Track, result.Warning);
            }
            else
                Update(item, ImportQueueStatus.Failed, result.Error ?? "Import failed");
        }
    }

    private void Update(ImportQueueItem item, ImportQueueStatus status, string? detail, int? trackId = null)
    {
        MusicLibraryService.Current.UpdateImportQueueItem(item.Id, status, detail, trackId);
        ItemUpdated?.Invoke(item with { Status = status, Detail = detail, TrackId = trackId ?? item.TrackId });
    }
}
