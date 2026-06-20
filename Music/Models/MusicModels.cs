namespace Music.Models;

public record MusicTrack(
    int Id, string CanonicalUrl, string Title, string FileName,
    int? RatingId, string DownloadedAt, int? DurationSeconds, bool NeedsReview);

public record Genre(int Id, string Name);
public record Style(int Id, string Name);
public record Rating(int Id, string Name, int SortOrder);
public record ModelGenre(int Id, string Name);
public record ModelSubgenre(int Id, int ModelGenreId, string Name);
public record StoredModelGenrePrediction(
    int ModelGenreId,
    string ModelGenreName,
    int ModelSubgenreId,
    string ModelSubgenreName,
    double Score);
public record GenreMapping(
    int Id,
    int GenreId,
    string GenreName,
    int ModelSubgenreId,
    int ModelGenreId,
    string ModelSubgenreName);
