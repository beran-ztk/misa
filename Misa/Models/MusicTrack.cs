namespace Misa.Models;

public record MusicTrack(int Id, string CanonicalUrl, string Title, string FileName, int GenreId, int RatingId, string DownloadedAt);
public record Genre(int Id, string Name);
public record Style(int Id, string Name);
public record Rating(int Id, string Name, int SortOrder);
