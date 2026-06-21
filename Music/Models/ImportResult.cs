using System;
using System.Collections.Generic;

namespace Music.Models;

public record ImportResult(
    bool Success,
    MusicTrack? Track = null,
    string? Error = null,
    string? Warning = null);

public enum ImportQueueStatus { Queued, Downloading, Analyzing, ReadyForReview, Failed, Skipped }

public record ImportPreviewItem(
    string SourceUrl,
    string CanonicalUrl,
    string Title,
    int? DurationSeconds,
    long? EstimatedSizeBytes,
    ImportQueueStatus Status,
    string? Detail = null);

public record ImportPreview(
    IReadOnlyList<ImportPreviewItem> Items,
    int ExistingCount,
    int DuplicateCount,
    int UnavailableCount,
    long? TotalEstimatedSizeBytes,
    TimeSpan? EstimatedDownloadTime,
    TimeSpan? EstimatedAnalysisTime);

public record ImportQueueItem(
    int Id,
    int BatchId,
    string SourceUrl,
    string CanonicalUrl,
    string Title,
    int? DurationSeconds,
    long? EstimatedSizeBytes,
    ImportQueueStatus Status,
    string? Detail,
    int? TrackId);

public record YouTubePlaylistEntry(string SourceUrl, string CanonicalUrl, string Title, int? DurationSeconds);
public record ImportQueueSummary(int Queued, int Downloading, int Analyzing, int ReadyForReview);
