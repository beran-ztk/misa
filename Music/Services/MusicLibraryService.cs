using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Music.Models;

namespace Music.Services;

public class MusicLibraryService
{
    public static readonly MusicLibraryService Current = new();
    private readonly MusicDatabase _db = new();
    private readonly TrackDownloadService _downloader = new();

    public void Initialize() => _db.Initialize();

    // --- Tracks ---

    public List<MusicTrack> GetTracks() => _db.GetAllTracks();

    public Dictionary<int, List<int>> GetAllTrackStyleIds() => _db.GetAllTrackStyleIds();
    public List<int> GetTrackStyleIds(int trackId) => _db.GetTrackStyleIds(trackId);

    public Dictionary<int, List<int>> GetAllTrackGenreIds() => _db.GetAllTrackGenreIds();
    public List<int> GetTrackGenreIds(int trackId) => _db.GetTrackGenreIds(trackId);
    public void UpdateTrack(int id, string title, List<int> genreIds, int ratingId, List<int> styleIds) 
        => _db.UpdateTrack(id, title, genreIds, ratingId, styleIds);

    // --- Lookups ---
    public List<Genre> GetGenres() => _db.GetGenres();
    public List<Style> GetStyles() => _db.GetStyles();
    public List<Rating> GetRatings() => _db.GetRatings();

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
        var duration = await _downloader.GetDurationAsync(filePath);
        _db.InsertTrack(canonicalUrl, _downloader.TitleFromFileName(fileName), fileName,
            request.GenreIds, request.RatingId, request.StyleIds, duration);

        return new DownloadResult(true);
    }
}
