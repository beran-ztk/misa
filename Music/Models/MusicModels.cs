using System.Collections.Generic;

namespace Music.Models;

public record MusicTrack(
    int Id, string CanonicalUrl, string Title, string FileName,
    int? RatingId, string DownloadedAt, int? DurationSeconds, bool NeedsReview,
    string? ChannelName, string? ChannelUrl, string? UploadedAt);

public record Genre(int Id, string Name);
public record TagCategory(int Id, string Name, string? Color, int SortOrder);
public record Tag(
    int Id,
    int CategoryId,
    string CategoryName,
    string? CategoryColor,
    string Name,
    string? Description);
public record TrackTag(
    int Id,
    int CategoryId,
    string CategoryName,
    string? CategoryColor,
    string Name,
    string? Description,
    string Source,
    double? Strength,
    double? Confidence);
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
    string CategoryName,
    string? CategoryColor,
    TagRuleMatchMode MatchMode,
    bool Enabled,
    IReadOnlyList<TagRuleCondition> Conditions);
public enum TagRuleMatchMode { All, Any }
public record TrackTagSuggestion(
    int RuleGroupId,
    int TagId,
    string TagName,
    string CategoryName,
    string? CategoryColor,
    TagRuleMatchMode MatchMode,
    string ConditionSummary,
    double Score,
    string State);
public record Style(int Id, string Name);
public record Rating(int Id, string Name, int SortOrder);
public record TrackUsageStats(int PlayCount, int ListenedSeconds, int SkipCount, string? LastListenedAt);
public record YouTubeTrackMetadata(
    string? Title,
    string? ChannelId,
    string? ChannelName,
    string? ChannelUrl,
    string? UploadedAt,
    long? EstimatedAudioSizeBytes = null,
    int? DurationSeconds = null);
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
public record ModelGenreReason(string ModelGenreName, string ModelSubgenreName, double Score);
public record TrackModelGenre(int GenreId, string GenreName, bool IsEnabled, IReadOnlyList<ModelGenreReason> Reasons);
