using System;
using System.Collections.Generic;
using System.Linq;
using Resona.Models;

namespace Resona.Services;

public static class TrackShuffleService
{
    private const double MinimumWeight = 0.50;
    private const double MaximumWeight = 3.50;

    public static Dictionary<int, double> CreatePriorities(
        IReadOnlyList<MusicTrack> tracks,
        IReadOnlyDictionary<int, TrackUsageStats> usageByTrackId,
        IReadOnlyList<Rating> ratings,
        Random random,
        DateTimeOffset now)
    {
        if (tracks.Count == 0)
            return [];

        var logarithmicListenCounts = tracks
            .Select(track => Math.Log(1d + UsageFor(track.Id, usageByTrackId).PlayCount))
            .ToList();
        var minimum = logarithmicListenCounts.Min();
        var average = logarithmicListenCounts.Average();
        var maximum = logarithmicListenCounts.Max();
        var ratingsById = ratings.ToDictionary(rating => rating.Id);

        var priorities = new Dictionary<int, double>(tracks.Count);
        for (var index = 0; index < tracks.Count; index++)
        {
            var track = tracks[index];
            var usage = UsageFor(track.Id, usageByTrackId);
            var listenFactor = ListenFactor(logarithmicListenCounts[index], minimum, average, maximum);
            var ratingFactor = track.RatingId is int ratingId && ratingsById.TryGetValue(ratingId, out var rating)
                ? RatingFactor(rating.SortOrder)
                : 1d;
            var recencyFactor = RecencyFactor(usage.LastListenedAt, now);
            var weight = Math.Clamp(listenFactor * ratingFactor * recencyFactor, MinimumWeight, MaximumWeight);

            // An exponential race produces a weighted random permutation without replacement.
            var sample = Math.Max(random.NextDouble(), double.Epsilon);
            priorities[track.Id] = -Math.Log(sample) / weight;
        }

        return priorities;
    }

    private static TrackUsageStats UsageFor(
        int trackId,
        IReadOnlyDictionary<int, TrackUsageStats> usageByTrackId) =>
        usageByTrackId.GetValueOrDefault(trackId, new TrackUsageStats(0, 0, 0, null));

    private static double ListenFactor(double value, double minimum, double average, double maximum)
    {
        if (value < average && average > minimum)
            return 1d + 0.35d * ((average - value) / (average - minimum));

        if (value > average && maximum > average)
            return 1d - 0.25d * ((value - average) / (maximum - average));

        return 1d;
    }

    private static double RatingFactor(int sortOrder) => sortOrder switch
    {
        <= 1 => 0.65d,
        2 => 0.95d,
        3 => 1.35d,
        4 => 1.90d,
        _ => 2.50d
    };

    private static double RecencyFactor(string? lastListenedAt, DateTimeOffset now)
    {
        if (!DateTimeOffset.TryParse(lastListenedAt, out var lastListened))
            return 1d;

        var ageInDays = Math.Max(0d, (now - lastListened.ToUniversalTime()).TotalDays);
        return 0.70d + 0.30d * Math.Clamp(ageInDays / 14d, 0d, 1d);
    }
}
