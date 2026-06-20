using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Music.Core;
using Music.Models;

namespace Music.Services;

public class MusicLibraryService
{
    public static readonly MusicLibraryService Current = new();
    private readonly MusicDatabase _db = new();
    private readonly TrackDownloadService _downloader = new();
    private readonly TrackAnalysisService _analysis = new();
    private readonly Dictionary<int, IReadOnlyList<ExperimentalAnalysisModel>> _experimentalAnalysis = [];

    public void Initialize() => _db.Initialize();

    // --- Tracks ---

    public List<MusicTrack> GetTracks() => _db.GetAllTracks();

    public Dictionary<int, List<int>> GetAllTrackStyleIds() => _db.GetAllTrackStyleIds();
    public List<int> GetTrackStyleIds(int trackId) => _db.GetTrackStyleIds(trackId);

    public Dictionary<int, List<int>> GetAllTrackGenreIds() => _db.GetAllTrackGenreIds();
    public Dictionary<int, List<int>> GetAllTrackModelGenreIds() => _db.GetAllTrackModelGenreIds();
    public Dictionary<int, List<int>> GetAllTrackManualGenreIds() => _db.GetAllTrackManualGenreIds();
    public List<int> GetTrackGenreIds(int trackId) => _db.GetTrackGenreIds(trackId);
    public List<int> GetTrackManualGenreIds(int trackId) => _db.GetTrackManualGenreIds(trackId);
    public List<TrackModelGenre> GetTrackModelGenres(int trackId) => _db.GetTrackModelGenres(trackId);
    public void SetTrackModelGenreEnabled(int trackId, int genreId, bool isEnabled) => _db.SetTrackModelGenreEnabled(trackId, genreId, isEnabled);
    public void UpdateTrack(int id, string title, List<int> genreIds, int? ratingId, List<int> styleIds)
        => _db.UpdateTrack(id, title, genreIds, ratingId, styleIds);
    public void SetTrackNeedsReview(int id, bool needsReview) => _db.SetTrackNeedsReview(id, needsReview);

    public Task<string?> DeleteTrackAsync(MusicTrack track)
    {
        try
        {
            var filePath = Path.Combine(Values.TracksDirectory, track.FileName);
            if (File.Exists(filePath))
                File.Delete(filePath);

            _db.DeleteTrack(track.Id);
            _experimentalAnalysis.Remove(track.Id);
            return Task.FromResult<string?>(null);
        }
        catch (Exception exception)
        {
            return Task.FromResult<string?>($"Could not delete track: {exception.Message}");
        }
    }

    // --- Lookups ---
    public List<Genre> GetGenres() => _db.GetGenres();
    public List<Style> GetStyles() => _db.GetStyles();
    public List<Rating> GetRatings() => _db.GetRatings();
    public List<ModelGenre> GetModelGenres() => _db.GetModelGenres();
    public List<ModelSubgenre> GetModelSubgenres(int? modelGenreId = null) => _db.GetModelSubgenres(modelGenreId);
    public List<StoredModelGenrePrediction> GetTrackGenrePredictions(int trackId) => _db.GetTrackGenrePredictions(trackId);
    public TrackAudioAnalysis? GetTrackAudioAnalysis(int trackId) => _db.GetTrackAudioAnalysis(trackId);
    public IReadOnlyList<ExperimentalAnalysisModel> GetExperimentalAnalysis(int trackId) =>
        _db.GetTrackAnalysisSignals(trackId);
    public List<DerivedTrackAttribute> GetTrackDerivedAttributes(int trackId) => _db.GetTrackDerivedAttributes(trackId);
    public void SetTrackDerivedAttributeOverride(int trackId, string key, string? value) =>
        _db.SetTrackDerivedAttributeOverride(trackId, key, value);
    public List<GenreMapping> GetGenreMappings() => _db.GetGenreMappings();
    public void SetGenreMapping(int genreId, int modelSubgenreId) => _db.SetGenreMapping(genreId, modelSubgenreId);
    public void RemoveGenreMapping(int modelSubgenreId) => _db.RemoveGenreMapping(modelSubgenreId);

    public bool TrackExistsByCanonicalUrl(string canonicalUrl) => _db.TrackExists(canonicalUrl);

    public Task<string?> GetRemoteTitleAsync(string canonicalUrl) => _downloader.GetTitleAsync(canonicalUrl);

    public async Task ExportPortableLibraryAsync(string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);
        var targetTracksDirectory = Path.Combine(targetDirectory, "tracks");
        var targetCoversDirectory = Path.Combine(targetDirectory, "covers");
        Directory.CreateDirectory(targetTracksDirectory);
        Directory.CreateDirectory(targetCoversDirectory);

        var tracks = GetTracks();
        var genres = GetGenres().ToDictionary(g => g.Id, g => g.Name);
        var styles = GetStyles().ToDictionary(s => s.Id, s => s.Name);
        var ratings = GetRatings().ToDictionary(r => r.Id, r => r.Name);
        var trackGenreIds = GetAllTrackGenreIds();
        var trackStyleIds = GetAllTrackStyleIds();

        var portableTracks = new List<PortableTrack>();

        foreach (var track in tracks)
        {
            var sourcePath = Path.Combine(Values.TracksDirectory, track.FileName);
            if (File.Exists(sourcePath))
                File.Copy(sourcePath, Path.Combine(targetTracksDirectory, track.FileName), overwrite: true);

            var coverFileName = ExportCover(sourcePath, targetCoversDirectory, track.FileName);

            portableTracks.Add(new PortableTrack(
                track.Title,
                track.FileName,
                track.DurationSeconds,
                track.RatingId is int ratingId ? ratings.GetValueOrDefault(ratingId, "") : "Not rated",
                NamesFor(trackGenreIds.GetValueOrDefault(track.Id, []), genres),
                NamesFor(trackStyleIds.GetValueOrDefault(track.Id, []), styles),
                coverFileName,
                track.NeedsReview));
        }

        await PortableLibraryStore.SaveAsync(
            targetDirectory,
            new PortableMusicLibrary(portableTracks, FilterPresetStore.Load()));
    }

    public async Task<DownloadResult> DownloadTrackAsync(DownloadRequest request, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(request.RawUrl))
            return new DownloadResult(false, "URL is required.");

        var videoId = YouTubeUrlNormalizer.ExtractVideoId(request.RawUrl);
        if (videoId == null)
            return new DownloadResult(false, "Could not parse YouTube URL.");

        var canonicalUrl = YouTubeUrlNormalizer.GetCanonicalUrl(videoId);

        if (_db.TrackExists(canonicalUrl))
            return new DownloadResult(false, "Track already exists.");

        var (success, errorOutput) = await DownloadWithSingleRetryAsync(canonicalUrl, progress);
        if (!success)
            return new DownloadResult(false, $"Failed:\n{errorOutput}");

        var filePath = _downloader.FindDownloadedFile(videoId);
        if (filePath == null)
            return new DownloadResult(false, "Download finished but file not found.");

        var fileName = Path.GetFileName(filePath);
        var duration = await _downloader.GetDurationAsync(filePath);
        var trackId = _db.InsertTrack(canonicalUrl, _downloader.TitleFromFileName(fileName), fileName,
            request.GenreIds, request.RatingId, request.StyleIds, duration);

        progress?.Report("Analyzing genres with Discogs-MAEST…");
        var (analysis, analysisError) = await _analysis.AnalyzeAsync(filePath);
        if (analysis is not null)
        {
            _db.SaveTrackAnalysis(trackId, analysis);
            CacheExperimentalAnalysis(trackId, analysis);
            return new DownloadResult(true);
        }

        _db.SetTrackNeedsReview(trackId, true);
        return new DownloadResult(true, Warning: $"Track downloaded, but analysis needs review: {analysisError}");
    }

    public async Task<ImportResult> ImportFromYouTubeAsync(string rawUrl, IProgress<string>? progress = null)
    {
        var videoId = YouTubeUrlNormalizer.ExtractVideoId(rawUrl);
        if (videoId is null)
            return new ImportResult(false, Error: "Could not parse YouTube URL.");

        var canonicalUrl = YouTubeUrlNormalizer.GetCanonicalUrl(videoId);
        if (_db.TrackExists(canonicalUrl))
            return new ImportResult(false, Error: "Track already exists.");

        var (success, errorOutput) = await DownloadWithSingleRetryAsync(canonicalUrl, progress);
        if (!success)
            return new ImportResult(false, Error: $"Download failed:\n{errorOutput}");

        var filePath = _downloader.FindDownloadedFile(videoId);
        if (filePath is null)
            return new ImportResult(false, Error: "Download finished but file not found.");

        var fileName = Path.GetFileName(filePath);
        var duration = await _downloader.GetDurationAsync(filePath);
        var trackId = _db.InsertTrack(canonicalUrl, _downloader.TitleFromFileName(fileName), fileName,
            [], null, [], duration);
        progress?.Report("Analyzing track…");
        var (analysis, analysisError) = await _analysis.AnalyzeAsync(filePath);
        if (analysis is not null)
        {
            _db.SaveTrackAnalysis(trackId, analysis);
            CacheExperimentalAnalysis(trackId, analysis);
        }
        else
            _db.SetTrackNeedsReview(trackId, true);

        return new ImportResult(
            true,
            GetTracks().Single(track => track.Id == trackId),
            Warning: analysis is null ? $"Track downloaded, but analysis needs review: {analysisError}" : null);
    }

    public async Task<string?> AnalyzeTrackAsync(MusicTrack track)
    {
        var filePath = Path.Combine(Values.TracksDirectory, track.FileName);
        var (analysis, error) = await _analysis.AnalyzeAsync(filePath);
        if (analysis is not null)
        {
            _db.SaveTrackAnalysis(track.Id, analysis);
            CacheExperimentalAnalysis(track.Id, analysis);
            return null;
        }

        _db.SetTrackNeedsReview(track.Id, true);
        return error ?? "Analysis failed.";
    }

    private void CacheExperimentalAnalysis(int trackId, TrackAnalysisResult analysis) =>
        _experimentalAnalysis[trackId] = analysis.ExperimentalModels ?? [];

    private async Task<(bool Success, string ErrorOutput)> DownloadWithSingleRetryAsync(
        string canonicalUrl,
        IProgress<string>? progress)
    {
        progress?.Report("Downloading audio…");
        var result = await _downloader.RunYtDlpAsync(canonicalUrl);
        if (result.Success || !IsForbiddenResponse(result.ErrorOutput))
            return result;

        progress?.Report("Download was rejected (403). Retrying once…");
        await Task.Delay(TimeSpan.FromMilliseconds(800));
        progress?.Report("Retrying download…");
        return await _downloader.RunYtDlpAsync(canonicalUrl);
    }

    private static bool IsForbiddenResponse(string output) =>
        output.Contains("403", StringComparison.OrdinalIgnoreCase)
        || output.Contains("forbidden", StringComparison.OrdinalIgnoreCase);

    private static List<string> NamesFor(IEnumerable<int> ids, IReadOnlyDictionary<int, string> names) =>
        ids.Select(id => names.GetValueOrDefault(id, ""))
            .Where(name => name.Length > 0)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string? ExportCover(string audioFilePath, string targetCoversDirectory, string trackFileName)
    {
        if (!File.Exists(audioFilePath))
            return null;

        var artwork = ThumbnailService.ReadEmbeddedArtworkFile(audioFilePath);
        if (artwork is null)
            return null;

        var coverFileName = SafeFileName(Path.GetFileNameWithoutExtension(trackFileName)) + artwork.Extension;
        File.WriteAllBytes(Path.Combine(targetCoversDirectory, coverFileName), artwork.Data);
        return coverFileName;
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value
            .Select(ch => invalid.Contains(ch) ? '_' : ch)
            .ToArray();

        var safe = new string(chars).Trim();
        return safe.Length == 0 ? "cover" : safe;
    }
}
