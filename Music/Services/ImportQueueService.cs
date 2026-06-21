using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public async Task<ImportPreview> PreviewAsync(IEnumerable<string> sourceUrls)
    {
        var previewItems = new List<ImportPreviewItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var existingCount = 0;
        var duplicateCount = 0;
        var unavailableCount = 0;

        foreach (var sourceUrl in sourceUrls.Select(url => url.Trim()).Where(url => url.Length > 0))
        {
            var entries = await _downloader.GetPlaylistEntriesAsync(sourceUrl);
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

                var metadata = await _downloader.GetMetadataAsync(entry.CanonicalUrl);
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
