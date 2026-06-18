using System.Text.Json;
using System.Text.Json.Serialization;

namespace Music.Core;

public sealed record PortableMusicLibrary(List<PortableTrack> Tracks)
{
    public static PortableMusicLibrary Empty { get; } = new([]);

    public IReadOnlyList<string> Ratings => Tracks
        .Select(t => t.Rating)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToList();

    public IReadOnlyList<string> Genres => Tracks
        .SelectMany(t => t.Genres)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToList();

    public IReadOnlyList<string> Styles => Tracks
        .SelectMany(t => t.Styles)
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToList();
}

public sealed record PortableTrack(
    string Title,
    string FileName,
    int? DurationSeconds,
    string Rating,
    List<string> Genres,
    List<string> Styles,
    string? CoverFileName = null)
{
    public string GenreText => string.Join(", ", Genres);
    public string StyleText => string.Join(", ", Styles);

    public string DurationText => DurationSeconds is int seconds
        ? $"{seconds / 60:D2}:{seconds % 60:D2}"
        : "";
}

public sealed record LoadedMusicLibrary(string RootDirectory, PortableMusicLibrary Library)
{
    public string TrackPath(PortableTrack track)
    {
        var tracksPath = Path.Combine(RootDirectory, "tracks", track.FileName);
        return File.Exists(tracksPath)
            ? tracksPath
            : Path.Combine(RootDirectory, track.FileName);
    }

    public string? CoverPath(PortableTrack track)
    {
        if (string.IsNullOrWhiteSpace(track.CoverFileName))
            return null;

        var path = Path.Combine(RootDirectory, "covers", track.CoverFileName);
        return File.Exists(path) ? path : null;
    }
}

public static class PortableLibraryStore
{
    public const string FileName = "library.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<LoadedMusicLibrary> LoadAsync(string rootDirectory)
    {
        Directory.CreateDirectory(rootDirectory);
        var path = Path.Combine(rootDirectory, FileName);
        if (!File.Exists(path))
            return new LoadedMusicLibrary(rootDirectory, PortableMusicLibrary.Empty);

        await using var stream = File.OpenRead(path);
        var library = await JsonSerializer.DeserializeAsync<PortableMusicLibrary>(stream, JsonOptions)
                      ?? PortableMusicLibrary.Empty;
        return new LoadedMusicLibrary(rootDirectory, library);
    }

    public static async Task SaveAsync(string rootDirectory, PortableMusicLibrary library)
    {
        Directory.CreateDirectory(rootDirectory);
        var path = Path.Combine(rootDirectory, FileName);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, library, JsonOptions);
    }
}

public sealed record PortableFilterGroup(IReadOnlySet<string> Genres, IReadOnlySet<string> Styles);

public static class PortableTrackFilter
{
    public static List<PortableTrack> Apply(
        IEnumerable<PortableTrack> tracks,
        string? searchText,
        IReadOnlySet<string> ratings,
        IReadOnlyList<PortableFilterGroup> filterGroups)
    {
        IEnumerable<PortableTrack> query = tracks;
        var term = searchText?.Trim();

        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(t => t.Title.Contains(term, StringComparison.OrdinalIgnoreCase));

        if (ratings.Count > 0)
            query = query.Where(t => ratings.Contains(t.Rating));

        var activeGroups = filterGroups
            .Where(g => g.Genres.Count > 0 || g.Styles.Count > 0)
            .ToList();

        if (activeGroups.Count > 0)
            query = query.Where(track => activeGroups.Any(group => MatchesGroup(track, group)));

        return query
            .OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool MatchesGroup(PortableTrack track, PortableFilterGroup group)
    {
        if (group.Genres.Count > 0 && !group.Genres.All(genre => track.Genres.Contains(genre, StringComparer.OrdinalIgnoreCase)))
            return false;

        if (group.Styles.Count > 0 && !group.Styles.All(style => track.Styles.Contains(style, StringComparer.OrdinalIgnoreCase)))
            return false;

        return true;
    }
}
