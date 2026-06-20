using System.Collections.Generic;

namespace Music.Models;

public record MusicTrack(
    int Id, string CanonicalUrl, string Title, string FileName,
    int? RatingId, string DownloadedAt, int? DurationSeconds, bool NeedsReview,
    string? ChannelName, string? ChannelUrl, string? UploadedAt);

public record Genre(int Id, string Name);
public record Style(int Id, string Name);
public record Rating(int Id, string Name, int SortOrder);
public record YouTubeTrackMetadata(string? Title, string? ChannelId, string? ChannelName, string? ChannelUrl, string? UploadedAt);
public record ModelGenre(int Id, string Name);
public record ModelSubgenre(int Id, int ModelGenreId, string Name);
public record StoredModelGenrePrediction(
    int ModelGenreId,
    string ModelGenreName,
    int ModelSubgenreId,
    string ModelSubgenreName,
    double Score);
public record ModelGenreReason(string ModelGenreName, string ModelSubgenreName, double Score);
public record TrackModelGenre(int GenreId, string GenreName, bool IsEnabled, IReadOnlyList<ModelGenreReason> Reasons);
public record GenreMapping(
    int Id,
    int GenreId,
    string GenreName,
    int ModelSubgenreId,
    int ModelGenreId,
    string ModelSubgenreName);
