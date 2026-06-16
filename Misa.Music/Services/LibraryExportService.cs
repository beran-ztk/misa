using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Misa.Music.Models;
using Misa.Shared.Models;

namespace Misa.Music.Services;

public class LibraryExportService
{
    private readonly MusicDatabase _db;
    private readonly string _musicDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public LibraryExportService(MusicDatabase db, string musicDirectory)
    {
        _db = db;
        _musicDirectory = musicDirectory;
    }

    public string ExportPath => Path.Combine(_musicDirectory, "library.json");

    public void Export()
    {
        var tracks = _db.GetAllTracks();
        var genres = _db.GetGenres().ToDictionary(g => g.Id, g => g.Name);
        var ratings = _db.GetRatings().ToDictionary(r => r.Id, r => r.Name);
        var styles = _db.GetStyles().ToDictionary(s => s.Id, s => s.Name);
        var languages = _db.GetLanguages().ToDictionary(l => l.Id, l => l.Name);
        var allStyleIds = _db.GetAllTrackStyleIds();
        var allGenreIds = _db.GetAllTrackGenreIds();
        var allLanguageIds = _db.GetAllTrackLanguageIds();

        var export = new LibraryExport
        {
            ExportedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            LibraryVersion = 1,
            Tracks = tracks.Select(t =>
            {
                var styleIds = allStyleIds.GetValueOrDefault(t.Id) ?? [];
                var genreIds = allGenreIds.GetValueOrDefault(t.Id) ?? [];
                var languageIds = allLanguageIds.GetValueOrDefault(t.Id) ?? [];
                return new LibraryTrack
                {
                    Id = t.Id,
                    Title = t.Title,
                    FileName = t.FileName,
                    CanonicalUrl = t.CanonicalUrl,
                    Genres = genreIds.Where(genres.ContainsKey).Select(id => genres[id]).ToList(),
                    Rating = ratings.GetValueOrDefault(t.RatingId, ""),
                    Styles = styleIds.Where(styles.ContainsKey).Select(id => styles[id]).ToList(),
                    Languages = languageIds.Where(languages.ContainsKey).Select(id => languages[id]).ToList(),
                    DurationSeconds = t.DurationSeconds,
                    Notes = t.Notes,
                    ReEvaluationNeeded = t.ReEvaluationNeeded,
                };
            }).ToList(),
        };

        File.WriteAllText(ExportPath, JsonSerializer.Serialize(export, JsonOptions));
    }
}
