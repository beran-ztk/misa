using System;
using System.Collections.Generic;
using System.Linq;
using Resona.Core;
using Resona.Models;

namespace Resona.Services;

public static class CloudDeviceLibrarySnapshotBuilder
{
    public const int CurrentSchemaVersion = 1;

    public static CloudDeviceLibrarySnapshot Build(MusicLibraryService library, CloudIdentity identity)
    {
        var sourceTracks = library.GetTracks();
        var ratings = library.GetRatings();
        var ratingNames = ratings.ToDictionary(item => item.Id, item => item.Name);
        var genres = library.GetGenres().ToDictionary(item => item.Id, item => item.Name);
        var styles = library.GetStyles().ToDictionary(item => item.Id, item => item.Name);
        var tags = library.GetTags().ToDictionary(item => item.Id, item => item.Name);
        var trackGenreIds = library.GetAllTrackGenreIds();
        var trackStyleIds = library.GetAllTrackStyleIds();
        var trackTagIds = library.GetAllTrackTagIds();
        var analyses = library.GetAllTrackAudioAnalyses();
        var emotionalCharacters = library.GetAllMirexScores();
        var usage = library.GetAllTrackUsageStats();

        var trackKeys = sourceTracks.ToDictionary(
            track => track.Id,
            track => TrackKey(track),
            EqualityComparer<int>.Default);
        var tracks = sourceTracks.Select(track =>
        {
            analyses.TryGetValue(track.Id, out var analysis);
            emotionalCharacters.TryGetValue(track.Id, out var emotional);
            usage.TryGetValue(track.Id, out var trackUsage);
            trackUsage ??= new TrackUsageStats(0, 0, 0, null);
            return new CloudDeviceTrack(
                trackKeys[track.Id],
                track.FileName,
                track.Title,
                track.OriginalTitle,
                track.Artist,
                track.Remix,
                track.Edits,
                track.DurationSeconds,
                track.RatingId is int ratingId ? ratingNames.GetValueOrDefault(ratingId) : null,
                track.RatingBand?.ToString(),
                Names(trackGenreIds.GetValueOrDefault(track.Id, []), genres),
                Names(trackStyleIds.GetValueOrDefault(track.Id, []), styles),
                Names(trackTagIds.GetValueOrDefault(track.Id, []), tags),
                track.LanguageCode,
                track.NeedsReview,
                track.LibraryState.ToString(),
                track.Thumbnail,
                trackUsage.PlayCount,
                trackUsage.ListenedSeconds,
                trackUsage.SkipCount,
                trackUsage.LastListenedAt,
                analysis is null
                    ? null
                    : new CloudPublicTrackAnalysis(
                        analysis.Bpm,
                        analysis.IntegratedLoudness,
                        analysis.LoudnessRange),
                emotional is null
                    ? new Dictionary<string, double>()
                    : emotional
                        .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(item => item.Key, item => item.Value),
                track.UpdatedAt);
        }).OrderBy(track => track.TrackKey, StringComparer.Ordinal).ToList();

        var collections = library.GetCollections()
            .Select(collection => new CloudDeviceCollection(
                collection.StableId,
                collection.Name,
                library.GetCollectionTrackIds(collection.Id)
                    .Where(trackKeys.ContainsKey)
                    .Select(trackId => trackKeys[trackId])
                    .ToList()))
            .ToList();

        return new CloudDeviceLibrarySnapshot(
            CurrentSchemaVersion,
            identity.UserId,
            tracks.Count,
            DateTime.UtcNow.ToString("O"),
            ratings
                .OrderBy(rating => rating.SortOrder)
                .Select(rating => new PortableRating(rating.Name, rating.SortOrder))
                .ToList(),
            FilterPresetStore.Load(),
            collections,
            tracks);
    }

    private static string TrackKey(MusicTrack track)
    {
        var key = string.IsNullOrWhiteSpace(track.SourceVideoId)
            ? YouTubeUrlNormalizer.ExtractVideoId(track.CanonicalUrl)
            : track.SourceVideoId.Trim();
        return !string.IsNullOrWhiteSpace(key)
            ? key
            : throw new InvalidOperationException($"Track {track.Id} has no stable source video ID.");
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
