using System.Collections.Generic;

namespace Resona.Models;

public sealed record CloudLibrarySnapshot(
    int SchemaVersion,
    CloudPublicProfile Profile,
    int TrackCount,
    string GeneratedAt,
    IReadOnlyList<CloudPublicTrack> Tracks);

public sealed record CloudPublicProfile(
    string UserId,
    string Username,
    string Bio,
    byte[]? ProfileImage,
    string UpdatedAt);

public sealed record CloudPublicTrack(
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

public sealed record CloudPublicTrackAnalysis(
    double? Bpm,
    double? IntegratedLoudness,
    double? LoudnessRange);
