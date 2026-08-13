using System;
using System.Collections.Generic;
using System.Linq;
using Resona.Models;

namespace Resona.Services;

public static class CloudLibrarySnapshotBuilder
{
    public const int CurrentSchemaVersion = 1;

    public static CloudLibrarySnapshot Build(MusicLibraryService library, CloudIdentity identity)
    {
        var tracks = library.GetTracksForLibraryView().Where(track => track.IsPublic).ToList();
        var ratings = library.GetRatings().ToDictionary(item => item.Id, item => item.Name);
        var tags = library.GetTags().ToDictionary(item => item.Id, item => item.Name);
        var genres = library.GetGenres().ToDictionary(item => item.Id, item => item.Name);
        var trackTagIds = library.GetAllTrackTagIds();
        var trackGenreIds = library.GetAllTrackGenreIds();
        var analyses = library.GetAllTrackAudioAnalyses();
        var emotionalCharacters = library.GetAllMirexScores();

        return Build(identity, tracks, ratings, tags, genres, trackTagIds, trackGenreIds, analyses, emotionalCharacters);
    }

    public static CloudLibrarySnapshot Build(
        CloudIdentity identity,
        IReadOnlyList<MusicTrack> tracks,
        IReadOnlyDictionary<int, string> ratings,
        IReadOnlyDictionary<int, string> tags,
        IReadOnlyDictionary<int, string> genres,
        IReadOnlyDictionary<int, List<int>> trackTagIds,
        IReadOnlyDictionary<int, List<int>> trackGenreIds,
        IReadOnlyDictionary<int, TrackAudioAnalysis> analyses,
        IReadOnlyDictionary<int, Dictionary<string, double>> emotionalCharacters)
    {
        var publicSourceTracks = tracks.Where(track => track.IsPublic).ToList();

        var publicTracks = publicSourceTracks.Select(track =>
        {
            var sourceVideoId = string.IsNullOrWhiteSpace(track.SourceVideoId)
                ? YouTubeUrlNormalizer.ExtractVideoId(track.CanonicalUrl)
                : track.SourceVideoId.Trim();
            if (string.IsNullOrWhiteSpace(sourceVideoId))
                throw new InvalidOperationException($"Public track {track.Id} has no valid YouTube video ID.");

            analyses.TryGetValue(track.Id, out var analysis);
            emotionalCharacters.TryGetValue(track.Id, out var emotionalCharacter);
            return new CloudPublicTrack(
                sourceVideoId,
                track.CanonicalUrl,
                track.Title,
                track.OriginalTitle,
                track.ChannelName,
                track.ChannelUrl,
                track.DurationSeconds,
                track.UploadedAt,
                track.SourceThumbnailUrl,
                track.RatingId is int ratingId ? ratings.GetValueOrDefault(ratingId) : null,
                track.LanguageCode,
                Names(trackTagIds.GetValueOrDefault(track.Id, []), tags),
                Names(trackGenreIds.GetValueOrDefault(track.Id, []), genres),
                analysis is null ? null : new CloudPublicTrackAnalysis(
                    analysis.Bpm, analysis.IntegratedLoudness, analysis.LoudnessRange),
                emotionalCharacter is null
                    ? new Dictionary<string, double>()
                    : emotionalCharacter
                        .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(item => EmotionalCharacterCatalog.Name(item.Key), item => item.Value),
                track.UpdatedAt);
        }).OrderBy(track => track.SourceVideoId, StringComparer.Ordinal).ToList();

        var snapshot = new CloudLibrarySnapshot(
            CurrentSchemaVersion,
            new CloudPublicProfile(
                identity.UserId,
                identity.Username,
                identity.Bio,
                identity.ProfileImage,
                identity.UpdatedAt),
            publicTracks.Count,
            DateTime.UtcNow.ToString("O"),
            publicTracks);
        Validate(snapshot, publicSourceTracks.Count);
        return snapshot;
    }

    public static void Validate(CloudLibrarySnapshot snapshot, int expectedPublicTrackCount)
    {
        if (snapshot.SchemaVersion != CurrentSchemaVersion)
            throw new InvalidOperationException($"Unsupported cloud snapshot version {snapshot.SchemaVersion}.");
        if (!Guid.TryParse(snapshot.Profile.UserId, out _))
            throw new InvalidOperationException("Cloud profile has no valid user ID.");
        if (string.IsNullOrWhiteSpace(snapshot.Profile.Username))
            throw new InvalidOperationException("Choose a cloud username before synchronizing.");
        if (snapshot.TrackCount != snapshot.Tracks.Count || snapshot.TrackCount != expectedPublicTrackCount)
            throw new InvalidOperationException("Cloud snapshot track count does not match the public library.");
        if (snapshot.Tracks.Any(track =>
                string.IsNullOrWhiteSpace(track.SourceVideoId)
                || string.IsNullOrWhiteSpace(track.CanonicalUrl)
                || string.IsNullOrWhiteSpace(track.Title)
                || string.IsNullOrWhiteSpace(track.OriginalTitle)))
            throw new InvalidOperationException("Cloud snapshot contains an incomplete public track.");
        if (snapshot.Tracks.GroupBy(track => track.SourceVideoId, StringComparer.Ordinal).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Cloud snapshot contains duplicate YouTube video IDs.");
    }

    private static IReadOnlyList<string> Names(
        IEnumerable<int> ids,
        IReadOnlyDictionary<int, string> names) => ids
        .Select(id => names.GetValueOrDefault(id))
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Select(name => name!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToList();
}
