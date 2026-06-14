using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Misa.Music.Models;

namespace Misa.Music.Services;

public class MusicLibraryService
{
    public static readonly MusicLibraryService Current = new();

    private MusicSettings _settings;
    private MusicDatabase _db;
    private TrackDownloadService _downloader;
    private TrackFileService _fileService;

    private MusicLibraryService()
    {
        _settings = MusicSettingsService.LoadSettings();
        _db = null!;
        _downloader = null!;
        _fileService = null!;
        ApplySettings();
    }

    private void ApplySettings()
    {
        var dbPath = Path.Combine(_settings.MusicDirectory, "music.db");
        _db = new MusicDatabase(dbPath);
        _downloader = new TrackDownloadService(_settings.ToolsDirectory, _settings.MusicDirectory, _settings);
        _fileService = new TrackFileService(_settings.MusicDirectory);
    }

    public MusicSettings GetSettings() => _settings;
    public string MusicDirectory => _settings.MusicDirectory;

    public void SaveSettings(MusicSettings settings)
    {
        _settings = settings;
        MusicSettingsService.SaveSettings(settings);
        ApplySettings();
    }

    public void Initialize() => _db.Initialize();

    // --- Tracks ---

    public List<MusicTrack> GetTracks() => _db.GetAllTracks();

    public Dictionary<int, List<int>> GetAllTrackStyleIds() => _db.GetAllMusicStyleIds();

    public List<int> GetTrackStyleIds(int trackId) => _db.GetMusicStyleIds(trackId);

    public async Task<DownloadResult> DownloadTrackAsync(DownloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RawUrl))
            return new DownloadResult(false, "URL is required.");

        var videoId = YouTubeUrlNormalizer.ExtractVideoId(request.RawUrl);
        if (videoId == null)
            return new DownloadResult(false, "Could not parse YouTube URL.");

        var canonicalUrl = YouTubeUrlNormalizer.GetCanonicalUrl(videoId);

        if (_db.TrackExists(canonicalUrl))
            return new DownloadResult(false, "Track already exists.");

        var (success, errorOutput) = await _downloader.RunYtDlpAsync(canonicalUrl);
        if (!success)
            return new DownloadResult(false, $"Failed:\n{errorOutput}");

        var filePath = _downloader.FindDownloadedFile(videoId);
        if (filePath == null)
            return new DownloadResult(false, "Download finished but file not found.");

        var fileName = Path.GetFileName(filePath);
        var title = string.IsNullOrWhiteSpace(request.CustomTitle)
            ? _downloader.TitleFromFileName(fileName)
            : request.CustomTitle.Trim();

        var duration = await _downloader.GetDurationAsync(filePath);
        _db.InsertTrack(canonicalUrl, title, fileName, request.GenreId, request.RatingId, request.StyleIds, duration);

        return new DownloadResult(true);
    }

    public void UpdateTrack(int id, string title, int genreId, int ratingId, List<int> styleIds, string? notes) =>
        _db.UpdateTrack(id, title, genreId, ratingId, styleIds, notes);

    public DeleteTrackResult DeleteTrack(int id, string fileName)
    {
        _db.DeleteTrack(id);
        return _fileService.TryDeleteFile(fileName);
    }

    // --- Genres ---

    public List<Genre> GetGenres() => _db.GetGenres();

    public void AddGenre(string name) => _db.InsertGenre(name);

    public void RenameGenre(int id, string name) => _db.UpdateGenre(id, name);

    public DeletionResult TryDeleteGenre(int id)
    {
        if (_db.IsGenreInUse(id))
            return new DeletionResult(false, "Cannot delete: genre is used by one or more tracks.");
        _db.DeleteGenre(id);
        return new DeletionResult(true);
    }

    // --- Styles ---

    public List<Style> GetStyles() => _db.GetStyles();

    public void AddStyle(string name) => _db.InsertStyle(name);

    public void RenameStyle(int id, string name) => _db.UpdateStyle(id, name);

    public DeletionResult TryDeleteStyle(int id)
    {
        if (_db.IsStyleInUse(id))
            return new DeletionResult(false, "Cannot delete: style is used by one or more tracks.");
        _db.DeleteStyle(id);
        return new DeletionResult(true);
    }

    // --- Ratings ---

    public List<Rating> GetRatings() => _db.GetRatings();

    public void AddRating(string name) => _db.InsertRating(name);

    public void RenameRating(int id, string name) => _db.UpdateRating(id, name);

    public DeletionResult TryDeleteRating(int id)
    {
        if (_db.IsRatingInUse(id))
            return new DeletionResult(false, "Cannot delete: rating is used by one or more tracks.");
        _db.DeleteRating(id);
        return new DeletionResult(true);
    }
}
