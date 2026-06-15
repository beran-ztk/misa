namespace Misa.Music.Models;

public record MusicTrack(int Id, string CanonicalUrl, string Title, string FileName, int RatingId, string DownloadedAt, int? DurationSeconds, string? Notes, bool ReEvaluationNeeded);
public record Genre(int Id, string Name);
public record Style(int Id, string Name);
public record Language(int Id, string Name);
public record Rating(int Id, string Name, int SortOrder);
