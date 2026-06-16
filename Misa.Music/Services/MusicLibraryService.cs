using System;
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
        // Re-save immediately so any fields added since the last run are written to disk
        // with their defaults, preventing them from falling back to C# class defaults on
        // the next startup when SavePlayerSettings is called before the user visits Settings.
        MusicSettingsService.SaveSettings(_settings);
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

    // Updates only player-related settings fields and flushes to disk.
    // Does not call ApplySettings() — the player state has no effect on the DB or downloader.
    public void SavePlayerSettings(int volume, bool isMuted, bool shuffleEnabled, bool autoplayEnabled,
        string repeatMode, bool crossfadeEnabled, bool showUpcomingTrackBar)
    {
        _settings.Volume = volume;
        _settings.IsMuted = isMuted;
        _settings.ShuffleEnabled = shuffleEnabled;
        _settings.AutoplayEnabled = autoplayEnabled;
        _settings.RepeatMode = repeatMode;
        _settings.CrossfadeEnabled = crossfadeEnabled;
        _settings.ShowUpcomingTrackBar = showUpcomingTrackBar;
        MusicSettingsService.SaveSettings(_settings);
    }

    // --- Tracks ---

    public List<MusicTrack> GetTracks() => _db.GetAllTracks();

    public Dictionary<int, List<int>> GetAllTrackStyleIds() => _db.GetAllTrackStyleIds();
    public List<int> GetTrackStyleIds(int trackId) => _db.GetTrackStyleIds(trackId);

    public Dictionary<int, List<int>> GetAllTrackGenreIds() => _db.GetAllTrackGenreIds();
    public List<int> GetTrackGenreIds(int trackId) => _db.GetTrackGenreIds(trackId);

    public Dictionary<int, List<int>> GetAllTrackLanguageIds() => _db.GetAllTrackLanguageIds();
    public List<int> GetTrackLanguageIds(int trackId) => _db.GetTrackLanguageIds(trackId);

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
        _db.InsertTrack(canonicalUrl, title, fileName,
            request.GenreIds, request.RatingId, request.StyleIds, request.LanguageIds, duration);

        return new DownloadResult(true);
    }

    public void UpdateTrack(int id, string title, List<int> genreIds, int ratingId,
                            List<int> styleIds, List<int> languageIds, string? notes, bool reEvaluationNeeded) =>
        _db.UpdateTrack(id, title, genreIds, ratingId, styleIds, languageIds, notes, reEvaluationNeeded);

    public void IncrementListenCount(int trackId) =>
        _db.IncrementListenCount(trackId, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

    public void IncrementSkipCount(int trackId) =>
        _db.IncrementSkipCount(trackId);

    public DeleteTrackResult DeleteTrack(int id, string fileName)
    {
        _db.DeleteTrack(id);
        return _fileService.TryDeleteFile(fileName);
    }

    // --- Thumbnails ---

    public string? EnsureThumbnailCached(int trackId, string fileName)
    {
        var audioFilePath = Path.Combine(_settings.MusicDirectory, fileName);
        return ThumbnailService.EnsureCached(trackId, audioFilePath);
    }

    public string? GetThumbnailCachePath(int trackId) =>
        ThumbnailService.GetCachedPath(trackId);

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

    // --- Languages ---

    public List<Language> GetLanguages() => _db.GetLanguages();
    public void AddLanguage(string name) => _db.InsertLanguage(name);
    public void RenameLanguage(int id, string name) => _db.UpdateLanguage(id, name);

    public DeletionResult TryDeleteLanguage(int id)
    {
        if (_db.IsLanguageInUse(id))
            return new DeletionResult(false, "Cannot delete: language is used by one or more tracks.");
        _db.DeleteLanguage(id);
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
