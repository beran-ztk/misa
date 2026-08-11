using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;

namespace Resona.Models;

public enum TrackLibraryState
{
    PendingRating,
    Active,
    Rejected
}

public sealed record BulkTrackDeleteResult(int Deleted, int FailedFiles, string? Error = null);

public record MusicTrack(
    int Id, string CanonicalUrl, string Title, string FileName,
    int? RatingId, string DownloadedAt, int? DurationSeconds, bool NeedsReview,
    string? ChannelName, string? ChannelUrl, string? UploadedAt, string UpdatedAt,
    bool AnalysisDisabled = false, bool IsPublic = false, byte[]? Thumbnail = null,
    TrackLibraryState LibraryState = TrackLibraryState.Active,
    string? SourceVideoId = null,
    long? ViewCount = null,
    long? LikeCount = null,
    string? SourceThumbnailUrl = null,
    string? SourceMetadataUpdatedAt = null,
    int? ChannelId = null);

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
public static class RatingNames
{
    public const string Avoid = "Avoid";
}
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
    int? DurationSeconds = null,
    long? ViewCount = null,
    long? LikeCount = null,
    long? ChannelFollowerCount = null,
    string? ThumbnailUrl = null);
public record YouTubeChannelSnapshot(
    string SourceUrl,
    string? ChannelId,
    string Name,
    string? ChannelUrl,
    IReadOnlyList<YouTubeChannelVideoEntry> Videos,
    string? ThumbnailUrl = null,
    byte[]? Thumbnail = null,
    long? FollowerCount = null);
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
    int UncheckedCount,
    bool AutoDownload,
    int QueuedDownloadCount,
    int ReadyDownloadCount,
    int DownloadingCount,
    int FailedDownloadCount,
    int SkippedDownloadCount,
    int NotQueuedDownloadCount)
{
    public string DownloadStateText => UncheckedCount == 0
        ? $"✓ All reviewed · {ReadyDownloadCount} ready"
        : DownloadingCount > 0 || QueuedDownloadCount > 0
            ? $"↓ Downloading · {ReadyDownloadCount} ready · {DownloadingCount} active · {QueuedDownloadCount} queued"
            : FailedDownloadCount > 0
                ? $"! Download stopped · {ReadyDownloadCount} ready · {FailedDownloadCount} failed"
                : NotQueuedDownloadCount > 0
                    ? $"○ Auto-download off · {NotQueuedDownloadCount} not downloaded"
                    : $"✓ Downloads complete · {ReadyDownloadCount} ready · {SkippedDownloadCount} skipped";
}
public sealed record ChannelHubItem(
    int Id,
    string Name,
    string SourceUrl,
    string? SourceChannelId,
    bool IsFollowed,
    bool NotificationsEnabled,
    bool AutoDownload,
    string? LastCheckedAt,
    int LocalTrackCount,
    int RatedTrackCount,
    double? AverageRating,
    int FavoriteCount,
    int GreatOrBetterCount,
    int PlayCount,
    int SkipCount,
    string? LastDownloadedAt,
    int KnownVideoCount,
    int UncheckedVideoCount,
    long? FollowerCount,
    int? MaxDurationMinutes,
    string? AutoDownloadFrom,
    IReadOnlyList<string> TopTracks,
    byte[]? Thumbnail = null,
    string? BasicMetadataCheckedAt = null)
{
    public Bitmap? Artwork { get; set; }
    public bool HasArtwork => Artwork is not null;
    public bool ShowMonogram => Artwork is null;
    public string Monogram
    {
        get
        {
            var parts = Name.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..System.Math.Min(2, parts[0].Length)].ToUpperInvariant(),
                _ => string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant()
            };
        }
    }

    public string TrackCountText => LocalTrackCount == 1 ? "1 track" : $"{LocalTrackCount} tracks";
    public string LibrarySummaryText => LocalTrackCount == 1
        ? RatedTrackCount == 1 ? "1 track · rated" : "1 track · unrated"
        : $"{LocalTrackCount:N0} tracks · {RatedTrackCount:N0} rated";
    public string LibraryDetailText
    {
        get
        {
            if (!System.DateTime.TryParse(LastDownloadedAt, out var downloadedAt))
                return RecommendationReason;
            return $"{RecommendationReason} · latest {downloadedAt.ToLocalTime():dd MMM yyyy}";
        }
    }
    public string RatingText => AverageRating is double average
        ? $"{average:0.0} avg · {RatedTrackCount} out of {LocalTrackCount} tracks rated"
        : "No ratings yet";
    public string QualitySummaryText => AverageRating is double average
        ? $"{average:0.0} average rating"
        : "No ratings yet";
    public string QualityCompactText => AverageRating is double average
        ? $"{average:0.0} avg · {RatedTrackCount:N0}/{LocalTrackCount:N0} rated"
        : "No ratings yet";
    public string QualityDetailText => AverageRating is null
        ? "No rated tracks"
        : $"{FavoriteCount:N0} favorite{(FavoriteCount == 1 ? string.Empty : "s")} · {GreatOrBetterCount:N0} great+";
    public string ActivityText => PlayCount == 0 ? "Not played yet" : $"{PlayCount} plays · {SkipCount} skips";
    public string ListeningSignalText => PlayCount <= 0
        ? "No listening history"
        : $"{System.Math.Clamp(SkipCount / (double)PlayCount * 100d, 0d, 100d):0}% skip rate";
    public string VideoSummaryText => KnownVideoCount == 1 ? "1 known video" : $"{KnownVideoCount:N0} known videos";
    public string VideoQueueText => UncheckedVideoCount == 0
        ? "Nothing new"
        : UncheckedVideoCount == 1 ? "1 new video" : $"{UncheckedVideoCount:N0} new videos";
    public string NewVideoText => UncheckedVideoCount == 1 ? "1 new video" : $"{UncheckedVideoCount} new videos";
    public bool HasNewVideos => UncheckedVideoCount > 0;
    public bool HasTopTracks => TopTracks.Count > 0;
    public string TopTracksText => string.Join("\n", TopTracks.Take(3));
    public string TopTracksInlineText => TopTracks.Count == 0
        ? "No local tracks yet"
        : string.Join("  ·  ", TopTracks.Take(3));
    public string FollowActionText => IsFollowed ? "Following" : "Follow";
    public string FollowGlyph => IsFollowed ? "×" : "+";
    public string FollowToolTip => IsFollowed ? "Unfollow channel" : "Follow channel";
    public string AutomationText => AutoDownload ? "Auto-download on" : "Manual downloads";
    public string DurationLimitText => MaxDurationMinutes is int minutes
        ? $"{minutes} min channel limit"
        : "Uses global duration limit";
    public string FollowerText => FollowerCount is long followers
        ? $"{FormatCompactNumber(followers)} YouTube followers"
        : string.Empty;
    public string ChannelMetadataText
    {
        get
        {
            var followerText = FollowerText.Length > 0 ? FollowerText : "Follower count unavailable";
            if (!System.DateTime.TryParse(LastCheckedAt, out var checkedAt))
                return $"{followerText} · not checked yet";
            return $"{followerText} · checked {checkedAt.ToLocalTime():dd MMM yyyy}";
        }
    }
    public string AutomationSummaryText => AutoDownload
        ? $"Auto-download on · {DurationLimitText}"
        : $"Auto-download off · {DurationLimitText}";

    public double RecommendationScore
    {
        get
        {
            var quality = AverageRating is double average ? (average - 1d) / 4d : 0.45d;
            var confidence = 1d - System.Math.Exp(-RatedTrackCount / 4d);
            var depth = 1d - System.Math.Exp(-LocalTrackCount / 5d);
            var engagement = PlayCount <= 0
                ? 0d
                : System.Math.Clamp(1d - SkipCount / (double)System.Math.Max(PlayCount, 1), 0d, 1d);
            return quality * 0.45d + confidence * 0.2d + depth * 0.2d + engagement * 0.15d;
        }
    }

    public string RecommendationReason => FavoriteCount > 0
        ? FavoriteCount == 1 ? "1 favorite in your library" : $"{FavoriteCount} favorites in your library"
        : GreatOrBetterCount > 0
            ? GreatOrBetterCount == 1 ? "1 highly rated track" : $"{GreatOrBetterCount} highly rated tracks"
            : LocalTrackCount == 1 ? "1 track in your library" : $"{LocalTrackCount} tracks in your library";

    private static string FormatCompactNumber(long value) => value switch
    {
        >= 1_000_000_000 => $"{value / 1_000_000_000d:0.#}B",
        >= 1_000_000 => $"{value / 1_000_000d:0.#}M",
        >= 1_000 => $"{value / 1_000d:0.#}K",
        _ => value.ToString()
    };
}
public sealed record ChannelNotification(
    int Id,
    int ChannelId,
    int? ChannelVideoId,
    string ChannelName,
    string Title,
    string CreatedAt,
    bool IsRead,
    string? CanonicalUrl)
{
    public string StateText => IsRead ? "Seen" : "New";
    public string CreatedText
    {
        get
        {
            if (!System.DateTime.TryParse(CreatedAt, out var created)) return string.Empty;
            var local = created.ToLocalTime();
            var age = System.DateTime.Now - local;
            if (age.TotalMinutes < 1) return "Just now";
            if (age.TotalHours < 1) return $"{System.Math.Max(1, (int)age.TotalMinutes)} min ago";
            if (age.TotalDays < 1) return $"{System.Math.Max(1, (int)age.TotalHours)} h ago";
            if (age.TotalDays < 7) return $"{System.Math.Max(1, (int)age.TotalDays)} d ago";
            return local.ToString("dd MMM yyyy");
        }
    }
}
public enum ChannelDownloadStatus { NotQueued, Queued, Downloading, Ready, Failed, Skipped }
public enum ChannelMetadataStatus { Pending, Queued, Loading, Ready, Failed }
public record ChannelVideo(
    int Id,
    int ChannelId,
    string VideoId,
    string CanonicalUrl,
    string Title,
    int? DurationSeconds,
    string? UploadedAt,
    string DiscoveredAt,
    bool IsChecked,
    ChannelDownloadStatus DownloadStatus,
    string? DownloadError,
    int DownloadAttempts,
    int? TrackId,
    ChannelMetadataStatus MetadataStatus = ChannelMetadataStatus.Pending,
    string? MetadataUpdatedAt = null,
    string? MetadataError = null,
    int MetadataAttempts = 0,
    long? ViewCount = null,
    long? LikeCount = null,
    string? ThumbnailUrl = null,
    TrackLibraryState? LibraryState = null,
    string? ChannelName = null,
    string? RatingName = null,
    int? RatingSortOrder = null,
    int ListenCount = 0,
    int ListenedSeconds = 0,
    int SkipCount = 0);
public record ChannelDownloadSummary(int Queued, int Downloading, int Ready, int Failed, int Skipped);
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
public record TrackModelGenre(
    int GenreId,
    string GenreName,
    bool IsEnabled,
    bool IsManual,
    IReadOnlyList<ModelGenreReason> Reasons);
