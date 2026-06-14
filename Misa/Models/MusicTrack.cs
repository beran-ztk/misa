namespace Misa.Models;

record MusicTrack(int Id, string CanonicalUrl, string Title, string FileName, int? GenreId, int? RatingId, string DownloadedAt);
record Genre(int Id, string Name);
record Style(int Id, string Name);
record Rating(int Id, string Name, int SortOrder);
