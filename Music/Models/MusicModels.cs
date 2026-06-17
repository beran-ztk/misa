namespace Music.Models;

public record MusicTrack(
    int Id, string CanonicalUrl, string Title, string FileName,
    int RatingId, string DownloadedAt, int? DurationSeconds,
    string? Notes, bool ReEvaluationNeeded,
    int ListenCount, int SkipCount, string? LastListenedAt);

public record Genre(int Id, string Name);
public record Style(int Id, string Name);
public record Rating(int Id, string Name, int SortOrder);
