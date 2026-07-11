using System.Collections.Generic;

namespace Music.Models;

public record MusicTrack(
    int Id, string CanonicalUrl, string Title, string FileName,
    int? RatingId, string DownloadedAt, int? DurationSeconds, bool NeedsReview,
    string? ChannelName, string? ChannelUrl, string? UploadedAt, string UpdatedAt,
    bool AnalysisDisabled = false, byte[]? Thumbnail = null);

public record Genre(int Id, string Name);
public record Tag(int Id, string Name);
public record TrackTag(int Id, string Name);
public record TagSignalSource(string ModelName, string SignalKey, string Description)
{
    public string DisplayName => $"{ModelName} · {SignalKey}";
}
public record TagRuleCondition(
    int Id,
    string SourceType,
    string SourceKey,
    double Threshold);
public record TagRuleGroup(
    int Id,
    int TagId,
    string TagName,
    TagRuleMatchMode MatchMode,
    bool Enabled,
    IReadOnlyList<TagRuleCondition> Conditions);
public enum TagRuleMatchMode { All, Any }
public record TrackTagSuggestion(
    int RuleGroupId,
    int TagId,
    string TagName,
    TagRuleMatchMode MatchMode,
    string ConditionSummary,
    double Score,
    string State);
public record Style(int Id, string Name);
public record Rating(int Id, string Name, int SortOrder);
public record TrackUsageStats(int PlayCount, int ListenedSeconds, int SkipCount, string? LastListenedAt);
public record PortableExportRecord(
    int Id,
    string ExportId,
    int SchemaVersion,
    string ExportedAt,
    int TrackCountTotal,
    int NewTrackCount,
    string? CutoffDownloadedAt,
    string? ArchivePath);
public record YouTubeTrackMetadata(
    string? Title,
    string? ChannelId,
    string? ChannelName,
    string? ChannelUrl,
    string? UploadedAt,
    long? EstimatedAudioSizeBytes = null,
    int? DurationSeconds = null);
public record YouTubeChannelSnapshot(
    string SourceUrl,
    string? ChannelId,
    string Name,
    string? ChannelUrl,
    IReadOnlyList<YouTubeChannelVideoEntry> Videos);
public record YouTubeChannelVideoEntry(
    string VideoId,
    string CanonicalUrl,
    string Title,
    int? DurationSeconds,
    string? UploadedAt);
public record ChannelSubscription(
    int Id,
    string Name,
    string SourceUrl,
    string? SourceChannelId,
    string? LastCheckedAt,
    int VideoCount,
    int UncheckedCount);
public record ChannelVideo(
    int Id,
    int ChannelId,
    string VideoId,
    string CanonicalUrl,
    string Title,
    int? DurationSeconds,
    string? UploadedAt,
    string DiscoveredAt,
    bool IsChecked);
public record ChannelRefreshResult(bool Success, int AddedCount, int UpdatedCount, string? Error = null);
public record ModelGenre(int Id, string Name);
public record ModelSubgenre(
    int Id,
    int ModelGenreId,
    string Name,
    string? Description = null,
    string? ClassificationHint = null,
    int? BpmMin = null,
    int? BpmMax = null);
public record ModelSubgenreDistinction(
    int ModelSubgenreId,
    int DistinguishFromModelSubgenreId,
    string ModelGenreName,
    string ModelSubgenreName,
    string Difference);
public record StoredModelGenrePrediction(
    int ModelGenreId,
    string ModelGenreName,
    int ModelSubgenreId,
    string ModelSubgenreName,
    double Score);
public record ManualModelGenreUsage(
    int ModelSubgenreId,
    int ModelGenreId,
    string ModelSubgenreName,
    string ModelGenreName,
    int UsageCount);
public record ModelGenreReason(string ModelGenreName, string ModelSubgenreName, double Score);
public record TrackModelGenre(int GenreId, string GenreName, bool IsEnabled, IReadOnlyList<ModelGenreReason> Reasons);
