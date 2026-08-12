using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Resona.Core;
using Resona.Models;

namespace Resona.Services;

public class MusicLibraryService
{
    public static readonly MusicLibraryService Current = new();
    private readonly MusicDatabase _db = new();
    private readonly TrackDownloadService _downloader = new();
    private readonly TrackAnalysisService _analysis = new();
    private readonly CanonicalUrlOperationCoordinator _trackOperations = new();
    private readonly Dictionary<int, IReadOnlyList<ExperimentalAnalysisModel>> _experimentalAnalysis = [];
    private int _channelMaxDownloadDurationMinutes = 12;

    public void Initialize()
    {
        if (Values.LibraryLocationsLoadError is not null)
            throw new InvalidOperationException(Values.LibraryLocationsLoadError);

        _channelMaxDownloadDurationMinutes = AppSettingsStore.Load().ChannelDownloadMaxDurationMinutes;
        _db.Initialize();
    }

    // --- Tracks ---

    public List<MusicTrack> GetTracks() => _db.GetAllTracks();
    public int CountUnratedTracks() => _db.CountUnratedTracks();
    public List<MusicTrack> GetUnratedTracks() => _db.GetUnratedTracks();
    public byte[]? GetTrackThumbnail(int trackId) => _db.GetTrackThumbnail(trackId);
    public List<MusicTrack> GetUnanalyzedTracks() => _db.GetUnanalyzedTracks();
    public MusicTrack? GetTrackById(int id) => _db.GetTrackById(id);
    public bool ShouldAnalyzeTrack(int id) =>
        GetTrackById(id) is { } track
        && TrackWorkflowPolicy.ShouldAnalyze(
            track.LibraryState,
            track.AnalysisDisabled,
            GetTrackAudioAnalysis(id) is not null);
    public MusicTrack? GetTrackByCanonicalUrl(string canonicalUrl) =>
        _db.GetTrackByCanonicalUrl(canonicalUrl);

    public Dictionary<int, List<int>> GetAllTrackStyleIds() => _db.GetAllTrackStyleIds();
    public List<int> GetTrackStyleIds(int trackId) => _db.GetTrackStyleIds(trackId);

    public Dictionary<int, List<int>> GetAllTrackGenreIds() => _db.GetAllTrackGenreIds();
    public List<int> GetTrackGenreIds(int trackId) => _db.GetTrackGenreIds(trackId);
    public List<TrackModelGenre> GetTrackModelGenres(int trackId) => _db.GetTrackModelGenres(trackId);
    public void SetTrackModelGenreEnabled(int trackId, int genreId, bool isEnabled) => _db.SetTrackModelGenreEnabled(trackId, genreId, isEnabled);
    public void UpdateTrack(int id, string title, List<int> genreIds, int? ratingId, List<int> styleIds, bool isPublic)
    {
        _db.UpdateTrack(id, title, genreIds, ratingId, styleIds, isPublic);
        if (ratingId is not null)
            BackgroundAnalysisService.Current.EnqueueTrack(id);
    }
    public void SetTrackRating(int id, int ratingId)
    {
        _db.SetTrackRating(id, ratingId);
        BackgroundAnalysisService.Current.EnqueueTrack(id);
        ChannelHubBackgroundService.Current.RequestRefresh();
    }
    public void SetTrackNeedsReview(int id, bool needsReview) => _db.SetTrackNeedsReview(id, needsReview);
    public void SetTrackAnalysisDisabled(int id, bool analysisDisabled)
    {
        _db.SetTrackAnalysisDisabled(id, analysisDisabled);
        if (!analysisDisabled)
            BackgroundAnalysisService.Current.EnqueueTrack(id);
    }
    public void RecordTrackPlaybackStarted(int trackId) => _db.RecordTrackPlaybackStarted(trackId);
    public void AddTrackListenedSeconds(int trackId, int seconds) => _db.AddTrackListenedSeconds(trackId, seconds);
    public void RecordTrackSkip(int trackId) => _db.RecordTrackSkip(trackId);
    public TrackUsageStats GetTrackUsageStats(int trackId) => _db.GetTrackUsageStats(trackId);
    public Dictionary<int, TrackUsageStats> GetAllTrackUsageStats() => _db.GetAllTrackUsageStats();
    public TimeSpan? EstimateAnalysisDuration(int? trackDurationSeconds, long? fileSizeBytes) =>
        _db.EstimateAnalysisDuration(trackDurationSeconds, fileSizeBytes);
    public TimeSpan? EstimateDownloadDuration(int? trackDurationSeconds, long? fileSizeBytes) =>
        _db.EstimateDownloadDuration(trackDurationSeconds, fileSizeBytes);
    public int CreateImportBatch(string sourceUrl, IReadOnlyList<ImportPreviewItem> items) => _db.CreateImportBatch(sourceUrl, items);
    public IReadOnlyList<MusicTrack> RecoverInterruptedImports()
    {
        var recoveredTracks = new List<MusicTrack>();
        foreach (var item in _db.GetInterruptedImportQueueItems())
        {
            var ownedTrack = item.TrackId is int trackId ? _db.GetTrackById(trackId) : null;
            if (ownedTrack is not null)
            {
                if (TrackFileExists(ownedTrack))
                {
                    _db.CompleteImportQueueItem(item.Id, ownedTrack.Id);
                    recoveredTracks.Add(_db.GetTrackById(ownedTrack.Id) ?? ownedTrack);
                }
                else
                {
                    WorkflowLog.Error("import", $"Owned track {ownedTrack.Id} has no media; cleaning queue item {item.Id} for retry.");
                    CleanupInterruptedImport(item);
                }
                continue;
            }

            // A track with the same URL may have been committed by another flow after
            // this queue item was created. It is not owned by this item and must never
            // be deleted during recovery.
            if (_db.GetTrackByCanonicalUrl(item.CanonicalUrl) is not null)
            {
                WorkflowLog.Info("import", $"Removed redundant queue item {item.Id}; its URL already belongs to another track.");
                _db.DeleteImportQueueItem(item.Id);
                continue;
            }

            if (item.Status is ImportQueueStatus.Downloading or ImportQueueStatus.Analyzing)
                CleanupInterruptedImport(item);
        }
        _db.RequeueInterruptedImports();
        return recoveredTracks;
    }
    public ImportQueueItem? GetNextQueuedImport() => _db.GetNextQueuedImport();
    public ImportQueueItem? ClaimNextQueuedImport() => _db.ClaimNextQueuedImport();
    public void UpdateImportQueueItem(int id, ImportQueueStatus status, string? detail = null, int? trackId = null) =>
        _db.UpdateImportQueueItem(id, status, detail, trackId);
    public ImportQueueSummary GetImportQueueSummary() => _db.GetImportQueueSummary();
    public HashSet<string> GetActiveImportCanonicalUrls() => _db.GetActiveImportCanonicalUrls();
    public List<ImportQueueSource> GetImportQueueSources() => _db.GetImportQueueSources();
    public bool RemoveQueuedImport(int id) => _db.RemoveQueuedImport(id);
    public void DeleteImportQueueItem(int id) => _db.DeleteImportQueueItem(id);
    public void CompleteImportQueueItem(int itemId, int trackId) => _db.CompleteImportQueueItem(itemId, trackId);

    public void CleanupInterruptedImport(ImportQueueItem item)
    {
        var videoId = YouTubeUrlNormalizer.ExtractVideoId(item.CanonicalUrl);
        if (!string.IsNullOrWhiteSpace(videoId))
            _downloader.DeleteDownloadArtifacts(videoId);

        // Only a track id persisted on this queue item establishes ownership.
        // Falling back to canonical URL can delete a legitimate track created by
        // another worker or ChannelHub during the interrupted import.
        var track = item.TrackId is int trackId ? GetTrackById(trackId) : null;

        if (track is null)
            return;

        try
        {
            var filePath = Path.Combine(Values.TracksDirectory, track.FileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch { }

        try { _db.DeleteTrack(track.Id); }
        catch { }
    }

    private static bool TrackFileExists(MusicTrack track) =>
        File.Exists(Path.Combine(Values.TracksDirectory, track.FileName));

    public Task<string?> DeleteTrackAsync(MusicTrack track)
    {
        try
        {
            var filePath = Path.Combine(Values.TracksDirectory, track.FileName);
            if (File.Exists(filePath))
                File.Delete(filePath);

            _db.DeleteTrack(track.Id);
            _experimentalAnalysis.Remove(track.Id);
            return Task.FromResult<string?>(null);
        }
        catch (Exception exception)
        {
            return Task.FromResult<string?>($"Could not delete track: {exception.Message}");
        }
    }

    public async Task<BulkTrackDeleteResult> DeleteAllUnratedTracksAsync()
    {
        var tracks = GetUnratedTracks();
        if (tracks.Count == 0)
            return new BulkTrackDeleteResult(0, 0);

        try
        {
            await Task.Run(() => _db.DeleteTracks(tracks.Select(track => track.Id).ToArray()));
        }
        catch (Exception exception)
        {
            return new BulkTrackDeleteResult(0, 0, exception.Message);
        }

        var failedFiles = await Task.Run(() =>
        {
            var failures = 0;
            foreach (var track in tracks)
            {
                try
                {
                    var filePath = Path.Combine(Values.TracksDirectory, track.FileName);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch
                {
                    failures++;
                }
            }
            return failures;
        });

        foreach (var track in tracks)
            _experimentalAnalysis.Remove(track.Id);

        return new BulkTrackDeleteResult(tracks.Count, failedFiles);
    }

    // --- Lookups ---
    public List<Genre> GetGenres() => _db.GetGenres();
    public List<Tag> GetTags() => _db.GetTags();
    public void AddTag(string name) => _db.AddTag(name);
    public void RenameTag(int id, string name) => _db.RenameTag(id, name);
    public string? DeleteTagIfUnused(int id) => _db.DeleteTagIfUnused(id);
    public Dictionary<int, List<int>> GetAllTrackTagIds() => _db.GetAllTrackTagIds();
    public List<int> GetTrackTagIds(int trackId) => _db.GetTrackTagIds(trackId);
    public List<TrackTag> GetTrackTags(int trackId) => _db.GetTrackTags(trackId);
    public void SetTrackManualTags(int trackId, IReadOnlyCollection<int> tagIds) => _db.SetTrackManualTags(trackId, tagIds);
    public List<TagSignalSource> GetTagSignalSources() => _db.GetTagSignalSources();
    public List<TagRuleGroup> GetTagRuleGroups() => _db.GetTagRuleGroups();
    public int CreateTagRuleGroup(int tagId, TagRuleMatchMode matchMode, string sourceType, string sourceKey, double threshold) =>
        _db.CreateTagRuleGroup(tagId, matchMode, sourceType, sourceKey, threshold);
    public void AddTagRuleCondition(int groupId, string sourceType, string sourceKey, double threshold) =>
        _db.AddTagRuleCondition(groupId, sourceType, sourceKey, threshold);
    public void DeleteTagRuleCondition(int conditionId) => _db.DeleteTagRuleCondition(conditionId);
    public void SetTagRuleGroupEnabled(int groupId, bool enabled) => _db.SetTagRuleGroupEnabled(groupId, enabled);
    public void SetTagRuleGroupMatchMode(int groupId, TagRuleMatchMode matchMode) =>
        _db.SetTagRuleGroupMatchMode(groupId, matchMode);
    public void DeleteTagRuleGroup(int groupId) => _db.DeleteTagRuleGroup(groupId);
    public void RefreshAllTagSuggestions() => _db.RefreshAllTagSuggestions();
    public List<TrackTagSuggestion> GetTrackTagSuggestions(int trackId) => _db.GetTrackTagSuggestions(trackId);
    public void AcceptTrackTagSuggestion(int trackId, int tagId, int ruleGroupId) =>
        _db.AcceptTrackTagSuggestion(trackId, tagId, ruleGroupId);
    public void RejectTrackTagSuggestion(int trackId, int ruleGroupId) =>
        _db.RejectTrackTagSuggestion(trackId, ruleGroupId);
    public List<Style> GetStyles() => _db.GetStyles();
    public List<Rating> GetRatings() => _db.GetRatings();
    public List<ModelGenre> GetModelGenres() => _db.GetModelGenres();
    public List<ModelSubgenre> GetModelSubgenres(int? modelGenreId = null) => _db.GetModelSubgenres(modelGenreId);
    public void AddModelSubgenre(int modelGenreId, string name) => _db.AddModelSubgenre(modelGenreId, name);
    public void UpdateModelSubgenre(int id, string name, string? description, string? classificationHint, int? bpmMin, int? bpmMax) =>
        _db.UpdateModelSubgenre(id, name, description, classificationHint, bpmMin, bpmMax);
    public List<ModelSubgenreDistinction> GetModelSubgenreDistinctions() => _db.GetModelSubgenreDistinctions();
    public List<ManualModelGenreUsage> GetTopManualModelGenres(int limit = 10) => _db.GetTopManualModelGenres(limit);
    public List<StoredModelGenrePrediction> GetTrackGenrePredictions(int trackId) => _db.GetTrackGenrePredictions(trackId);
    public TrackAudioAnalysis? GetTrackAudioAnalysis(int trackId) => _db.GetTrackAudioAnalysis(trackId);
    public Dictionary<int, TrackAudioAnalysis> GetAllTrackAudioAnalyses() => _db.GetAllTrackAudioAnalyses();
    public Dictionary<int, Dictionary<string, double>> GetAllMirexScores() => _db.GetAllMirexScores();
    public IReadOnlyList<ExperimentalAnalysisModel> GetExperimentalAnalysis(int trackId) =>
        _db.GetTrackAnalysisSignals(trackId);

    public bool TrackExistsByCanonicalUrl(string canonicalUrl) => _db.TrackExists(canonicalUrl);

    public Task<string?> GetRemoteTitleAsync(string canonicalUrl) => _downloader.GetTitleAsync(canonicalUrl);

    public List<ChannelSubscription> GetChannelSubscriptions() => _db.GetChannelSubscriptions();
    public List<ChannelHubItem> GetChannelHubItems() => _db.GetChannelHubItems();
    public List<ChannelVideo> GetChannelVideos(int channelId) => _db.GetChannelVideos(channelId);
    public void RecoverChannelMetadataQueue() => _db.RecoverChannelMetadataQueue();
    public void PrepareLibraryMetadataBackfill() => _db.PrepareLibraryMetadataBackfill();
    public void EnsureChannelMetadataQueueIndexes() => _db.EnsureChannelMetadataQueueIndexes();
    public int QueueChannelVideoMetadata(int channelId, int limit) => _db.QueueChannelVideoMetadata(channelId, limit);
    public bool QueueSpecificChannelVideoMetadata(int videoId) => _db.QueueSpecificChannelVideoMetadata(videoId);
    public int QueueAutoDownloadMetadata(int limit) => _db.QueueAutoDownloadMetadata(limit);
    public int QueueBackgroundChannelVideoMetadata(int limit) => _db.QueueBackgroundChannelVideoMetadata(limit);
    public int CountBackgroundChannelVideoMetadataWork() => _db.CountBackgroundChannelVideoMetadataWork();
    public ChannelVideo? ClaimNextChannelVideoMetadata() => _db.ClaimNextChannelVideoMetadata();
    public bool HasQueuedChannelVideoMetadata() => _db.HasQueuedChannelVideoMetadata();
    public int ResetChannelMetadataIssues(int channelId) => _db.ResetChannelMetadataIssues(channelId);
    public bool RetryChannelVideoIssue(int videoId)
    {
        if (!_db.RetryChannelVideoIssue(videoId))
            return false;

        ChannelMetadataService.Current.NotifyQueueChanged();
        ChannelDownloadService.Current.NotifyQueueChanged();
        return true;
    }
    public Task<YouTubeTrackMetadata?> GetChannelVideoMetadataAsync(
        string canonicalUrl,
        CancellationToken cancellationToken = default) =>
        _downloader.GetMetadataAsync(canonicalUrl, cancellationToken);
    public void CompleteChannelVideoMetadata(int videoId, YouTubeTrackMetadata? metadata, string? error) =>
        _db.CompleteChannelVideoMetadata(
            videoId,
            metadata,
            error,
            _channelMaxDownloadDurationMinutes);
    public void SetChannelFollowed(int channelId, bool followed)
    {
        _db.SetChannelFollowed(channelId, followed);
        if (followed)
            ChannelMetadataService.Current.RequestAllChannels();
    }
    public void MarkChannelBasicMetadataChecked(int channelId) => _db.MarkChannelBasicMetadataChecked(channelId);
    public List<ChannelNotification> GetChannelNotifications() => _db.GetChannelNotifications();
    public int GetUnreadChannelNotificationCount() => _db.GetUnreadChannelNotificationCount();
    public void MarkChannelNotificationRead(int notificationId) => _db.MarkChannelNotificationRead(notificationId);
    public void ArchiveChannelNotification(int notificationId) => _db.ArchiveChannelNotification(notificationId);
    public void SetChannelAutoDownload(int channelId, bool enabled)
    {
        _db.SetChannelAutoDownload(
            channelId,
            enabled,
            _channelMaxDownloadDurationMinutes);
        if (enabled)
            ChannelMetadataService.Current.RequestChannel(channelId, 1);
        ChannelDownloadService.Current.NotifyQueueChanged();
    }

    public int GetChannelMaxDownloadDurationMinutes() => _channelMaxDownloadDurationMinutes;
    public void SetChannelMaxDownloadDuration(int channelId, int? maxDurationMinutes)
    {
        _db.SetChannelMaxDownloadDuration(channelId, maxDurationMinutes, _channelMaxDownloadDurationMinutes);
        ChannelDownloadService.Current.NotifyQueueChanged();
    }
    public void SetGlobalChannelMaxDownloadDuration(int maxDurationMinutes)
    {
        maxDurationMinutes = Math.Clamp(maxDurationMinutes, 1, 24 * 60);
        _channelMaxDownloadDurationMinutes = maxDurationMinutes;
        AppSettingsStore.SaveChannelDownloadMaxDurationMinutes(maxDurationMinutes);
        _db.SetGlobalChannelMaxDownloadDuration(maxDurationMinutes);
        ChannelDownloadService.Current.NotifyQueueChanged();
    }
    public void RecoverChannelDownloads() => _db.RecoverChannelDownloads(_channelMaxDownloadDurationMinutes);
    public ChannelVideo? ClaimNextChannelDownload() => _db.ClaimNextChannelDownload(_channelMaxDownloadDurationMinutes);
    public void CompleteChannelDownload(int videoId, bool success, string? error) =>
        _db.CompleteChannelDownload(videoId, success, error);
    public ChannelDownloadSummary GetChannelDownloadSummary() => _db.GetChannelDownloadSummary();
    public bool RequestChannelVideoDownload(int videoId)
    {
        if (!_db.RequestChannelVideoDownload(videoId))
            return false;
        ChannelMetadataService.Current.RequestVideo(videoId);
        ChannelDownloadService.Current.NotifyQueueChanged();
        return true;
    }
    public bool DeleteChannel(int channelId) => _db.DeleteChannel(channelId);

    public async Task<(MusicTrack? Track, string? Error)> PreloadChannelVideoAsync(ChannelVideo video)
    {
        using var operation = await _trackOperations.AcquireAsync(video.CanonicalUrl);
        if (_db.GetTrackByCanonicalUrl(video.CanonicalUrl) is { } existingTrack)
            return (existingTrack, null);

        var stopwatch = Stopwatch.StartNew();
        var download = await _downloader.DownloadChannelTrackAsync(video.CanonicalUrl, video.VideoId);
        if (!download.Success || download.FilePath is null)
            return (null, string.IsNullOrWhiteSpace(download.ErrorOutput)
                ? "Channel audio download failed."
                : download.ErrorOutput.Trim());

        var duration = video.DurationSeconds ?? await _downloader.GetDurationAsync(download.FilePath);
        var thumbnail = ThumbnailService.ReadEmbeddedArtworkThumbnail(download.FilePath);
        var trackId = _db.InsertPreloadedChannelTrack(
            video,
            Path.GetFileName(download.FilePath),
            duration,
            new FileInfo(download.FilePath).Length,
            (int)stopwatch.ElapsedMilliseconds,
            thumbnail);
        return (GetTrackById(trackId), null);
    }

    public MusicTrack? ConfirmChannelVideo(int videoId)
    {
        var trackId = _db.CompleteChannelVideoReview(videoId, skip: false);
        if (trackId is not int id)
            return null;
        BackgroundAnalysisService.Current.EnqueueTrack(id);
        return GetTrackById(id);
    }

    public MusicTrack? SkipChannelVideo(int videoId)
    {
        var trackId = _db.CompleteChannelVideoReview(videoId, skip: true);
        return trackId is int id ? GetTrackById(id) : null;
    }

    public bool DismissChannelVideo(int videoId) => _db.DismissChannelVideo(videoId);

    public HashSet<int> GetTrackIdsMissingAnalysis() => _db.GetTrackIdsMissingAnalysis();

    public async Task<ChannelRefreshResult> AddOrRefreshChannelAsync(
        string rawUrl,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return new ChannelRefreshResult(false, 0, 0, "Channel URL is required.");

        progress?.Report("Reading channel…");
        var (snapshot, error) = await _downloader.GetChannelSnapshotAsync(rawUrl.Trim(), cancellationToken);
        if (snapshot is null)
            return new ChannelRefreshResult(false, 0, 0, error ?? "Could not read channel.");
        if (snapshot.Videos.Count == 0)
            return new ChannelRefreshResult(false, 0, 0, "Channel was read, but no videos were returned.");

        if (!string.IsNullOrWhiteSpace(snapshot.ThumbnailUrl))
        {
            progress?.Report("Downloading channel icon…");
            snapshot = snapshot with
            {
                Thumbnail = await _downloader.DownloadImageAsync(snapshot.ThumbnailUrl, cancellationToken)
            };
        }

        progress?.Report($"Saving {snapshot.Videos.Count} videos…");
        var result = await Task.Run(
            () => _db.SaveChannelSnapshot(snapshot),
            cancellationToken);
        ChannelMetadataService.Current.RequestAllChannels();
        ChannelDownloadService.Current.NotifyQueueChanged();
        return result;
    }

    public async Task<ChannelRefreshResult> RefreshChannelAsync(
        ChannelSubscription channel,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default) =>
        await AddOrRefreshChannelAsync(channel.SourceUrl, progress, cancellationToken);

    public async Task<int> RefreshSubscribedChannelsAsync(CancellationToken cancellationToken = default)
    {
        var added = 0;
        foreach (var channel in GetChannelSubscriptions())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await RefreshChannelAsync(channel, cancellationToken: cancellationToken);
            if (result.Success)
                added += result.AddedCount;
        }
        return added;
    }

    public async Task<string> ExportPortableLibraryAsync(string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);
        var exportId = Guid.NewGuid().ToString("N");
        var exportedAt = DateTime.UtcNow.ToString("O");
        var tempDirectory = Path.Combine(targetDirectory, $".portable-export-{exportId}");
        var targetTracksDirectory = Path.Combine(tempDirectory, "tracks");
        var targetCoversDirectory = Path.Combine(tempDirectory, "covers");
        Directory.CreateDirectory(targetTracksDirectory);
        Directory.CreateDirectory(targetCoversDirectory);

        var allTracks = GetTracks();
        var analyzedTrackIds = _db.GetAnalyzedTrackIds();
        var tracks = allTracks
            .Where(track => analyzedTrackIds.Contains(track.Id))
            .ToList();
        var lastExport = _db.GetLastPortableExport();
        var previousLibrary = await LoadPreviousPortableExportAsync(lastExport);
        var previousTracksByFileName = (previousLibrary?.Tracks ?? [])
            .GroupBy(track => track.FileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var newMediaTrackIds = tracks
            .Where(track => IsNewPortableMedia(track, lastExport, previousLibrary, previousTracksByFileName))
            .Select(track => track.Id)
            .ToHashSet();
        var cutoffDownloadedAt = allTracks
            .Select(track => track.DownloadedAt)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .DefaultIfEmpty(lastExport?.CutoffDownloadedAt)
            .Max(StringComparer.Ordinal);
        var genres = GetGenres().ToDictionary(g => g.Id, g => g.Name);
        var tags = GetTags().ToDictionary(t => t.Id, t => t.Name);
        var styles = GetStyles().ToDictionary(s => s.Id, s => s.Name);
        var ratingDefinitions = GetRatings();
        var ratings = ratingDefinitions.ToDictionary(r => r.Id, r => r.Name);
        var trackGenreIds = GetAllTrackGenreIds();
        var trackTagIds = GetAllTrackTagIds();
        var trackStyleIds = GetAllTrackStyleIds();

        var portableTracks = new List<PortableTrack>();

        try
        {
            foreach (var track in tracks)
            {
                var sourcePath = Path.Combine(Values.TracksDirectory, track.FileName);
                if (newMediaTrackIds.Contains(track.Id) && File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, Path.Combine(targetTracksDirectory, track.FileName), overwrite: true);
                }

                var isNewMediaTrack = newMediaTrackIds.Contains(track.Id);
                var coverFileName = isNewMediaTrack
                    ? ExportCover(sourcePath, targetCoversDirectory, track.FileName)
                    : previousTracksByFileName.GetValueOrDefault(track.FileName)?.CoverFileName;
                var thumbnail = _db.GetTrackThumbnail(track.Id);
                if (thumbnail is not { Length: > 0 } && File.Exists(sourcePath))
                    thumbnail = ThumbnailService.ReadEmbeddedArtworkThumbnail(sourcePath);
                var usage = GetTrackUsageStats(track.Id);

                portableTracks.Add(new PortableTrack(
                    track.Title,
                    track.FileName,
                    track.DurationSeconds,
                    track.RatingId is int ratingId ? ratings.GetValueOrDefault(ratingId, "") : "None",
                    NamesFor(trackGenreIds.GetValueOrDefault(track.Id, []), genres),
                    NamesFor(trackStyleIds.GetValueOrDefault(track.Id, []), styles),
                    coverFileName,
                    track.NeedsReview,
                    NamesFor(trackTagIds.GetValueOrDefault(track.Id, []), tags),
                    track.DownloadedAt,
                    track.ChannelName,
                    track.ChannelUrl,
                    track.UploadedAt,
                    usage.PlayCount,
                    usage.ListenedSeconds,
                    usage.SkipCount,
                    usage.LastListenedAt,
                    thumbnail,
                    track.IsPublic));
            }

            await PortableLibraryStore.SaveAsync(
                tempDirectory,
                new PortableMusicLibrary(
                    portableTracks,
                    FilterPresetStore.Load(),
                    PortableMusicLibrary.CurrentSchemaVersion,
                    exportId,
                    exportedAt,
                    "incremental",
                    ratingDefinitions
                        .Select(rating => new PortableRating(rating.Name, rating.SortOrder))
                        .ToList()));

            var archivePath = NextExportArchivePath(targetDirectory, exportedAt);
            ZipFile.CreateFromDirectory(tempDirectory, archivePath, CompressionLevel.Fastest, includeBaseDirectory: false);
            _db.RecordPortableExport(
                exportId,
                PortableMusicLibrary.CurrentSchemaVersion,
                exportedAt,
                tracks.Count,
                newMediaTrackIds.Count,
                cutoffDownloadedAt,
                archivePath);
            return archivePath;
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    public async Task<DownloadResult> DownloadTrackAsync(DownloadRequest request, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(request.RawUrl))
            return new DownloadResult(false, "URL is required.");

        var videoId = YouTubeUrlNormalizer.ExtractVideoId(request.RawUrl);
        if (videoId == null)
            return new DownloadResult(false, "Could not parse YouTube URL.");

        var canonicalUrl = YouTubeUrlNormalizer.GetCanonicalUrl(videoId);

        using var operation = await _trackOperations.AcquireAsync(canonicalUrl);

        if (_db.TrackExists(canonicalUrl))
            return new DownloadResult(false, "Track already exists.");

        var result = await DownloadAndPersistYouTubeTrackAsync(
            videoId,
            canonicalUrl,
            request.GenreIds,
            request.RatingId,
            request.StyleIds,
            progress);
        if (!result.Success || result.Track is null)
            return new DownloadResult(false, result.Error, result.Warning);

        if (ShouldAnalyzeTrack(result.Track.Id))
            BackgroundAnalysisService.Current.EnqueueTrack(result.Track.Id);
        return new DownloadResult(true, Warning: result.Warning);
    }

    public async Task<ImportResult> ImportFromYouTubeAsync(
        string rawUrl,
        IProgress<string>? progress = null,
        Action<int>? trackCreated = null)
    {
        var videoId = YouTubeUrlNormalizer.ExtractVideoId(rawUrl);
        if (videoId is null)
            return new ImportResult(false, Error: "Could not parse YouTube URL.");

        var canonicalUrl = YouTubeUrlNormalizer.GetCanonicalUrl(videoId);
        using var operation = await _trackOperations.AcquireAsync(canonicalUrl);
        if (_db.TrackExists(canonicalUrl))
            return new ImportResult(false, Error: "Track already exists.");

        var result = await DownloadAndPersistYouTubeTrackAsync(
            videoId,
            canonicalUrl,
            [],
            null,
            [],
            progress);
        if (result.Success && result.Track is not null)
            trackCreated?.Invoke(result.Track.Id);
        return result;
    }

    private async Task<ImportResult> DownloadAndPersistYouTubeTrackAsync(
        string videoId,
        string canonicalUrl,
        List<int> genreIds,
        int? ratingId,
        List<int> styleIds,
        IProgress<string>? progress)
    {
        _downloader.DeleteDownloadArtifacts(videoId);

        progress?.Report("Checking audio details…");
        var previewMetadata = await _downloader.GetMetadataAsync(canonicalUrl);
        var downloadEstimate = EstimateDownloadDuration(
            previewMetadata?.DurationSeconds,
            previewMetadata?.EstimatedAudioSizeBytes);
        var downloadStopwatch = Stopwatch.StartNew();
        var (success, errorOutput) = await DownloadWithSingleRetryAsync(canonicalUrl, progress, downloadEstimate);
        if (!success)
            return new ImportResult(false, Error: $"Download failed:\n{errorOutput}");

        var filePath = _downloader.FindDownloadedFile(videoId);
        if (filePath is null)
            return new ImportResult(false, Error: "Download finished but file not found.");

        try
        {
            var fileName = Path.GetFileName(filePath);
            var duration = await _downloader.GetDurationAsync(filePath);
            var metadata = previewMetadata ?? await _downloader.GetMetadataAsync(canonicalUrl);
            var fileSizeBytes = new FileInfo(filePath).Length;
            var thumbnail = ThumbnailService.ReadEmbeddedArtworkThumbnail(filePath) ?? [];
            var trackId = _db.InsertTrack(
                canonicalUrl,
                metadata?.Title ?? _downloader.TitleFromFileName(fileName),
                fileName,
                genreIds,
                ratingId,
                styleIds,
                duration,
                fileSizeBytes,
                (int)downloadStopwatch.ElapsedMilliseconds,
                metadata,
                thumbnail);
            var track = GetTrackById(trackId)
                ?? throw new InvalidOperationException($"Track {trackId} was inserted but could not be reloaded.");
            ChannelHubBackgroundService.Current.RequestRefresh();
            WorkflowLog.Info("download", $"Persisted track {trackId} from a YouTube download.");
            return new ImportResult(true, track);
        }
        catch (Exception exception)
        {
            WorkflowLog.Error("download", "Could not persist downloaded track.", exception);
            try
            {
                File.Delete(filePath);
            }
            catch (Exception cleanupException)
            {
                WorkflowLog.Error("download", "Could not remove an unowned download after persistence failed.", cleanupException);
            }

            return new ImportResult(false, Error: "The audio was downloaded but could not be added to the library.");
        }
    }

    public async Task<(string? Error, bool Retryable)> AnalyzeTrackAsync(
        MusicTrack track,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(Values.TracksDirectory, track.FileName);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await _analysis.AnalyzeTrackAsync(filePath, cancellationToken);
            var analysis = TrackAnalysisService.ToTrackAnalysisResult(response);
            _db.SaveTrackAnalysis(track.Id, analysis, (int)stopwatch.ElapsedMilliseconds);
            CacheExperimentalAnalysis(track.Id, analysis);
            return (null, false);
        }
        catch (MusicAnalysisException exception)
        {
            if (exception.Kind is MusicAnalysisErrorKind.FileError or MusicAnalysisErrorKind.InvalidResponse)
                _db.MarkTrackAnalysisFailed(track.Id);
            return (exception.Message,
                exception.Kind is MusicAnalysisErrorKind.ConnectionError or MusicAnalysisErrorKind.Timeout);
        }
    }

    private void CacheExperimentalAnalysis(int trackId, TrackAnalysisResult analysis) =>
        _experimentalAnalysis[trackId] = analysis.ExperimentalModels ?? [];

    private string AnalysisProgressText(int? durationSeconds, long fileSizeBytes)
    {
        var estimate = EstimateAnalysisDuration(durationSeconds, fileSizeBytes);
        return estimate is null
            ? "Analyzing track…"
            : $"Analyzing track… usually about {FormatEstimate(estimate.Value)}";
    }

    private static string FormatEstimate(TimeSpan duration) => duration.TotalMinutes >= 1
        ? $"{Math.Ceiling(duration.TotalMinutes):0} min"
        : $"{Math.Max(1, Math.Round(duration.TotalSeconds)):0} sec";

    private async Task<(bool Success, string ErrorOutput)> DownloadWithSingleRetryAsync(
        string canonicalUrl,
        IProgress<string>? progress,
        TimeSpan? estimate = null)
    {
        progress?.Report(estimate is null
            ? "Downloading audio…"
            : $"Downloading audio… usually about {FormatEstimate(estimate.Value)}");
        var result = await _downloader.RunYtDlpAsync(canonicalUrl);
        if (result.Success || !IsForbiddenResponse(result.ErrorOutput))
            return result;

        progress?.Report("Download was rejected (403). Retrying once…");
        await Task.Delay(TimeSpan.FromMilliseconds(800));
        progress?.Report("Retrying download…");
        return await _downloader.RunYtDlpAsync(canonicalUrl);
    }

    private static bool IsForbiddenResponse(string output) =>
        output.Contains("403", StringComparison.OrdinalIgnoreCase)
        || output.Contains("forbidden", StringComparison.OrdinalIgnoreCase);

    private static List<string> NamesFor(IEnumerable<int> ids, IReadOnlyDictionary<int, string> names) =>
        ids.Select(id => names.GetValueOrDefault(id, ""))
            .Where(name => name.Length > 0)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static async Task<PortableMusicLibrary?> LoadPreviousPortableExportAsync(PortableExportRecord? previousExport)
    {
        if (string.IsNullOrWhiteSpace(previousExport?.ArchivePath) || !File.Exists(previousExport.ArchivePath))
            return null;

        try
        {
            using var archive = ZipFile.OpenRead(previousExport.ArchivePath);
            var manifest = archive.Entries.FirstOrDefault(entry =>
                string.Equals(entry.FullName.Replace('\\', '/'), PortableLibraryStore.FileName, StringComparison.OrdinalIgnoreCase));
            if (manifest is null)
                return null;

            await using var stream = manifest.Open();
            return await PortableLibraryStore.LoadAsync(stream);
        }
        catch
        {
            // If the previous archive was moved or damaged, a complete export is safer than
            // producing a manifest whose newly eligible tracks have no accompanying media.
            return null;
        }
    }

    private static bool IsNewPortableMedia(
        MusicTrack track,
        PortableExportRecord? previousExport,
        PortableMusicLibrary? previousLibrary,
        IReadOnlyDictionary<string, PortableTrack> previousTracksByFileName)
    {
        if (previousLibrary is not null)
            return !previousTracksByFileName.ContainsKey(track.FileName);

        if (previousExport is null)
            return true;

        // Older installations may no longer have the recorded archive at its original path.
        // UpdatedAt is metadata-only and must never cause the media file to be exported again.
        return string.IsNullOrWhiteSpace(previousExport.CutoffDownloadedAt)
               || string.CompareOrdinal(track.DownloadedAt, previousExport.CutoffDownloadedAt) > 0;
    }

    private static string? ExportCover(string audioFilePath, string targetCoversDirectory, string trackFileName)
    {
        if (!File.Exists(audioFilePath))
            return null;

        var artwork = ThumbnailService.ReadEmbeddedArtworkFile(audioFilePath);
        if (artwork is null)
            return null;

        var coverFileName = SafeFileName(Path.GetFileNameWithoutExtension(trackFileName)) + artwork.Extension;
        File.WriteAllBytes(Path.Combine(targetCoversDirectory, coverFileName), artwork.Data);
        return coverFileName;
    }

    private static string NextExportArchivePath(string targetDirectory, string exportedAt)
    {
        var timestamp = DateTime.Parse(exportedAt, null, System.Globalization.DateTimeStyles.RoundtripKind)
            .ToLocalTime()
            .ToString("yyyyMMdd-HHmmss");
        var archivePath = Path.Combine(targetDirectory, $"MusicLibrary-{timestamp}.zip");
        if (!File.Exists(archivePath))
            return archivePath;

        return Path.Combine(targetDirectory, $"MusicLibrary-{timestamp}-{Guid.NewGuid():N}.zip");
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value
            .Select(ch => invalid.Contains(ch) ? '_' : ch)
            .ToArray();

        var safe = new string(chars).Trim();
        return safe.Length == 0 ? "cover" : safe;
    }
}
