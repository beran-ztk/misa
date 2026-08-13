using System.Collections.Generic;

namespace Resona.Models;

public sealed record CloudPage<T>(
    IReadOnlyList<T> Items,
    int Offset,
    int Limit,
    long Total);

public sealed record CloudPublicProfileSummary(
    string UserId,
    string Username,
    string Bio,
    bool HasProfileImage,
    int TrackCount,
    string UpdatedAt,
    string? SynchronizedAt);

public sealed record CloudPublicLibraryTrack(
    string SourceVideoId,
    string CanonicalUrl,
    string Title,
    string OriginalTitle,
    string? ChannelName,
    string? ChannelUrl,
    int? DurationSeconds,
    string? UploadedAt,
    string? ThumbnailUrl,
    string? Rating,
    string? LanguageCode,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Genres,
    CloudPublicTrackAnalysis? Analysis,
    IReadOnlyDictionary<string, double> EmotionalCharacter,
    string UpdatedAt);
