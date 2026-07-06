using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Music.Core;
using Music.Models;

namespace Music.Services;

public class MusicLibraryService
{
    public static readonly MusicLibraryService Current = new();
    private readonly MusicDatabase _db = new();
    private readonly TrackDownloadService _downloader = new();
    private readonly TrackAnalysisService _analysis = new();
    private readonly Dictionary<int, IReadOnlyList<ExperimentalAnalysisModel>> _experimentalAnalysis = [];

    public void Initialize() => _db.Initialize();

    // --- Tracks ---

    public List<MusicTrack> GetTracks() => _db.GetAllTracks();
    public byte[]? GetTrackThumbnail(int trackId) => _db.GetTrackThumbnail(trackId);
    public List<MusicTrack> GetUnanalyzedTracks() => _db.GetUnanalyzedTracks();
    public MusicTrack? GetTrackById(int id) => _db.GetTrackById(id);
    public bool ShouldAnalyzeTrack(int id) =>
        GetTrackById(id) is { AnalysisDisabled: false } && GetTrackAudioAnalysis(id) is null;
    public MusicTrack? GetTrackByCanonicalUrl(string canonicalUrl) =>
        GetTracks().FirstOrDefault(track => string.Equals(track.CanonicalUrl, canonicalUrl, StringComparison.OrdinalIgnoreCase));

    public Dictionary<int, List<int>> GetAllTrackStyleIds() => _db.GetAllTrackStyleIds();
    public List<int> GetTrackStyleIds(int trackId) => _db.GetTrackStyleIds(trackId);

    public Dictionary<int, List<int>> GetAllTrackGenreIds() => _db.GetAllTrackGenreIds();
    public List<int> GetTrackGenreIds(int trackId) => _db.GetTrackGenreIds(trackId);
    public List<TrackModelGenre> GetTrackModelGenres(int trackId) => _db.GetTrackModelGenres(trackId);
    public void SetTrackModelGenreEnabled(int trackId, int genreId, bool isEnabled) => _db.SetTrackModelGenreEnabled(trackId, genreId, isEnabled);
    public void UpdateTrack(int id, string title, List<int> genreIds, int? ratingId, List<int> styleIds)
        => _db.UpdateTrack(id, title, genreIds, ratingId, styleIds);
    public void SetTrackNeedsReview(int id, bool needsReview) => _db.SetTrackNeedsReview(id, needsReview);
    public void SetTrackAnalysisDisabled(int id, bool analysisDisabled) => _db.SetTrackAnalysisDisabled(id, analysisDisabled);
    public void RecordTrackPlaybackStarted(int trackId) => _db.RecordTrackPlaybackStarted(trackId);
    public void AddTrackListenedSeconds(int trackId, int seconds) => _db.AddTrackListenedSeconds(trackId, seconds);
    public void RecordTrackSkip(int trackId) => _db.RecordTrackSkip(trackId);
    public TrackUsageStats GetTrackUsageStats(int trackId) => _db.GetTrackUsageStats(trackId);
    public TimeSpan? EstimateAnalysisDuration(int? trackDurationSeconds, long? fileSizeBytes) =>
        _db.EstimateAnalysisDuration(trackDurationSeconds, fileSizeBytes);
    public TimeSpan? EstimateDownloadDuration(int? trackDurationSeconds, long? fileSizeBytes) =>
        _db.EstimateDownloadDuration(trackDurationSeconds, fileSizeBytes);
    public int CreateImportBatch(string sourceUrl, IReadOnlyList<ImportPreviewItem> items) => _db.CreateImportBatch(sourceUrl, items);
    public void RequeueInterruptedImports()
    {
        foreach (var item in _db.GetInterruptedImportQueueItems())
            if (item.Status == ImportQueueStatus.Downloading)
                CleanupInterruptedImport(item);
        _db.RequeueInterruptedImports();
    }
    public ImportQueueItem? GetNextQueuedImport() => _db.GetNextQueuedImport();
    public void UpdateImportQueueItem(int id, ImportQueueStatus status, string? detail = null, int? trackId = null) =>
        _db.UpdateImportQueueItem(id, status, detail, trackId);
    public ImportQueueSummary GetImportQueueSummary() => _db.GetImportQueueSummary();
    public HashSet<string> GetActiveImportCanonicalUrls() => _db.GetActiveImportCanonicalUrls();
    public List<ImportQueueSource> GetImportQueueSources() => _db.GetImportQueueSources();
    public bool RemoveQueuedImport(int id) => _db.RemoveQueuedImport(id);
    public void DeleteImportQueueItem(int id) => _db.DeleteImportQueueItem(id);

    public void CleanupInterruptedImport(ImportQueueItem item)
    {
        var videoId = YouTubeUrlNormalizer.ExtractVideoId(item.CanonicalUrl);
        if (!string.IsNullOrWhiteSpace(videoId))
            _downloader.DeleteDownloadArtifacts(videoId);

        var track = item.TrackId is int trackId
            ? GetTrackById(trackId)
            : GetTrackByCanonicalUrl(item.CanonicalUrl);
        if (track is null && item.TrackId is int)
            track = GetTrackByCanonicalUrl(item.CanonicalUrl);

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

    // --- Lookups ---
    public List<Genre> GetGenres() => _db.GetGenres();
    public List<TagCategory> GetTagCategories() => _db.GetTagCategories();
    public void AddTagCategory(string name) => _db.AddTagCategory(name);
    public void RenameTagCategory(int id, string name) => _db.RenameTagCategory(id, name);
    public void SetTagCategoryColor(int id, string? color) => _db.SetTagCategoryColor(id, color);
    public string? DeleteTagCategoryIfUnused(int id) => _db.DeleteTagCategoryIfUnused(id);
    public List<Tag> GetTags() => _db.GetTags();
    public void AddTag(int categoryId, string name, string? description) => _db.AddTag(categoryId, name, description);
    public void RenameTag(int id, string name, string? description) => _db.RenameTag(id, name, description);
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
    public IReadOnlyList<ExperimentalAnalysisModel> GetExperimentalAnalysis(int trackId) =>
        _db.GetTrackAnalysisSignals(trackId);
    public List<DerivedTrackAttribute> GetTrackDerivedAttributes(int trackId) => _db.GetTrackDerivedAttributes(trackId);
    public void SetTrackDerivedAttributeOverride(int trackId, string key, string? value) =>
        _db.SetTrackDerivedAttributeOverride(trackId, key, value);

    public bool TrackExistsByCanonicalUrl(string canonicalUrl) => _db.TrackExists(canonicalUrl);

    public Task<string?> GetRemoteTitleAsync(string canonicalUrl) => _downloader.GetTitleAsync(canonicalUrl);

    public List<ChannelSubscription> GetChannelSubscriptions() => _db.GetChannelSubscriptions();
    public List<ChannelVideo> GetChannelVideos(int channelId) => _db.GetChannelVideos(channelId);
    public void SetChannelVideoChecked(int videoId, bool isChecked) => _db.SetChannelVideoChecked(videoId, isChecked);
    public bool DeleteChannel(int channelId) => _db.DeleteChannel(channelId);

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

        progress?.Report($"Saving {snapshot.Videos.Count} videos…");
        return _db.SaveChannelSnapshot(snapshot);
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

    public async Task ExportPortableLibraryAsync(string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);
        var targetTracksDirectory = Path.Combine(targetDirectory, "tracks");
        var targetCoversDirectory = Path.Combine(targetDirectory, "covers");
        Directory.CreateDirectory(targetTracksDirectory);
        Directory.CreateDirectory(targetCoversDirectory);

        var tracks = GetTracks();
        var genres = GetGenres().ToDictionary(g => g.Id, g => g.Name);
        var tags = GetTags().ToDictionary(t => t.Id, t => $"{t.CategoryName}: {t.Name}");
        var styles = GetStyles().ToDictionary(s => s.Id, s => s.Name);
        var ratings = GetRatings().ToDictionary(r => r.Id, r => r.Name);
        var trackGenreIds = GetAllTrackGenreIds();
        var trackTagIds = GetAllTrackTagIds();
        var trackStyleIds = GetAllTrackStyleIds();

        var portableTracks = new List<PortableTrack>();

        foreach (var track in tracks)
        {
            var sourcePath = Path.Combine(Values.TracksDirectory, track.FileName);
            if (File.Exists(sourcePath))
                File.Copy(sourcePath, Path.Combine(targetTracksDirectory, track.FileName), overwrite: true);

            var coverFileName = ExportCover(sourcePath, targetCoversDirectory, track.FileName);

            portableTracks.Add(new PortableTrack(
                track.Title,
                track.FileName,
                track.DurationSeconds,
                track.RatingId is int ratingId ? ratings.GetValueOrDefault(ratingId, "") : "Not rated",
                NamesFor(trackGenreIds.GetValueOrDefault(track.Id, []), genres),
                NamesFor(trackStyleIds.GetValueOrDefault(track.Id, []), styles),
                coverFileName,
                track.NeedsReview,
                NamesFor(trackTagIds.GetValueOrDefault(track.Id, []), tags)));
        }

        await PortableLibraryStore.SaveAsync(
            targetDirectory,
            new PortableMusicLibrary(portableTracks, FilterPresetStore.Load()));
    }

    public async Task<DownloadResult> DownloadTrackAsync(DownloadRequest request, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(request.RawUrl))
            return new DownloadResult(false, "URL is required.");

        var videoId = YouTubeUrlNormalizer.ExtractVideoId(request.RawUrl);
        if (videoId == null)
            return new DownloadResult(false, "Could not parse YouTube URL.");

        var canonicalUrl = YouTubeUrlNormalizer.GetCanonicalUrl(videoId);

        if (_db.TrackExists(canonicalUrl))
            return new DownloadResult(false, "Track already exists.");

        _downloader.DeleteDownloadArtifacts(videoId);

        progress?.Report("Checking audio details…");
        var previewMetadata = await _downloader.GetMetadataAsync(canonicalUrl);
        var downloadEstimate = EstimateDownloadDuration(previewMetadata?.DurationSeconds, previewMetadata?.EstimatedAudioSizeBytes);
        var downloadStopwatch = Stopwatch.StartNew();
        var (success, errorOutput) = await DownloadWithSingleRetryAsync(canonicalUrl, progress, downloadEstimate);
        if (!success)
            return new DownloadResult(false, $"Failed:\n{errorOutput}");

        var filePath = _downloader.FindDownloadedFile(videoId);
        if (filePath == null)
            return new DownloadResult(false, "Download finished but file not found.");

        var fileName = Path.GetFileName(filePath);
        var duration = await _downloader.GetDurationAsync(filePath);
        var metadata = previewMetadata ?? await _downloader.GetMetadataAsync(canonicalUrl);
        var fileSizeBytes = new FileInfo(filePath).Length;
        var thumbnail = ThumbnailService.ReadEmbeddedArtworkThumbnail(filePath) ?? [];
        var trackId = _db.InsertTrack(canonicalUrl, metadata?.Title ?? _downloader.TitleFromFileName(fileName), fileName,
            request.GenreIds, request.RatingId, request.StyleIds, duration, fileSizeBytes, (int)downloadStopwatch.ElapsedMilliseconds, metadata, thumbnail);

        BackgroundAnalysisService.Current.EnqueueTrack(trackId);
        return new DownloadResult(true);
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
        if (_db.TrackExists(canonicalUrl))
            return new ImportResult(false, Error: "Track already exists.");

        _downloader.DeleteDownloadArtifacts(videoId);

        progress?.Report("Checking audio details…");
        var previewMetadata = await _downloader.GetMetadataAsync(canonicalUrl);
        var downloadEstimate = EstimateDownloadDuration(previewMetadata?.DurationSeconds, previewMetadata?.EstimatedAudioSizeBytes);
        var downloadStopwatch = Stopwatch.StartNew();
        var (success, errorOutput) = await DownloadWithSingleRetryAsync(canonicalUrl, progress, downloadEstimate);
        if (!success)
            return new ImportResult(false, Error: $"Download failed:\n{errorOutput}");

        var filePath = _downloader.FindDownloadedFile(videoId);
        if (filePath is null)
            return new ImportResult(false, Error: "Download finished but file not found.");

        var fileName = Path.GetFileName(filePath);
        var duration = await _downloader.GetDurationAsync(filePath);
        var metadata = previewMetadata ?? await _downloader.GetMetadataAsync(canonicalUrl);
        var fileSizeBytes = new FileInfo(filePath).Length;
        var thumbnail = ThumbnailService.ReadEmbeddedArtworkThumbnail(filePath) ?? [];
        var trackId = _db.InsertTrack(canonicalUrl, metadata?.Title ?? _downloader.TitleFromFileName(fileName), fileName,
            [], null, [], duration, fileSizeBytes, (int)downloadStopwatch.ElapsedMilliseconds, metadata, thumbnail);
        trackCreated?.Invoke(trackId);
        BackgroundAnalysisService.Current.EnqueueTrack(trackId);

        return new ImportResult(
            true,
            GetTracks().Single(track => track.Id == trackId));
    }

    public async Task<string?> AnalyzeTrackAsync(MusicTrack track)
    {
        var filePath = Path.Combine(Values.TracksDirectory, track.FileName);
        var stopwatch = Stopwatch.StartNew();
        var (analysis, error) = await _analysis.AnalyzeAsync(filePath);
        if (analysis is not null)
        {
            _db.SaveTrackAnalysis(track.Id, analysis, (int)stopwatch.ElapsedMilliseconds);
            CacheExperimentalAnalysis(track.Id, analysis);
            return null;
        }

        _db.SetTrackNeedsReview(track.Id, true);
        _db.SetTrackAnalysisDisabled(track.Id, true);
        return error ?? "Analysis failed.";
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
