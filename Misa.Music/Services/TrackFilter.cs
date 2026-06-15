using System;
using System.Collections.Generic;
using System.Linq;
using Misa.Music.Models;

namespace Misa.Music.Services;

public enum TrackSortField { Title, Rating, DownloadedAt, Duration }
public enum TrackSortDirection { Ascending, Descending }

// A single filter group: within the group all conditions are AND; between groups is OR.
public record FilterGroup(IReadOnlySet<int> GenreIds, IReadOnlySet<int> StyleIds);

public static class TrackFilter
{
    public static List<MusicTrack> Apply(
        IEnumerable<MusicTrack> tracks,
        IReadOnlyDictionary<int, List<int>> trackGenreIds,
        IReadOnlyDictionary<int, List<int>> trackStyleIds,
        IReadOnlyDictionary<int, int> ratingSortOrders,
        IReadOnlySet<int> ratingFilter,
        IReadOnlyList<FilterGroup> filterGroups,
        bool? reEvaluationFilter,
        string? searchText,
        TrackSortField sortField,
        TrackSortDirection sortDirection)
    {
        IEnumerable<MusicTrack> query = tracks;

        if (ratingFilter.Count > 0)
            query = query.Where(t => ratingFilter.Contains(t.RatingId));

        if (reEvaluationFilter.HasValue)
            query = query.Where(t => t.ReEvaluationNeeded == reEvaluationFilter.Value);

        var term = searchText?.Trim();
        if (!string.IsNullOrEmpty(term))
            query = query.Where(t => MatchesSearch(t, term));

        // Apply filter groups: OR between groups, AND within a group.
        // Empty groups (no genres and no styles selected) are ignored.
        var activeGroups = filterGroups
            .Where(g => g.GenreIds.Count > 0 || g.StyleIds.Count > 0)
            .ToList();

        if (activeGroups.Count > 0)
        {
            var seen = new HashSet<int>();
            var matched = new List<MusicTrack>();
            foreach (var track in query)
            {
                if (seen.Contains(track.Id)) continue;
                if (activeGroups.Any(g => MatchesGroup(track, g, trackGenreIds, trackStyleIds)))
                {
                    seen.Add(track.Id);
                    matched.Add(track);
                }
            }
            query = matched;
        }

        IEnumerable<MusicTrack> sorted = sortField switch
        {
            TrackSortField.Title => sortDirection == TrackSortDirection.Ascending
                ? query.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase)
                : query.OrderByDescending(t => t.Title, StringComparer.OrdinalIgnoreCase),
            TrackSortField.Rating => sortDirection == TrackSortDirection.Ascending
                ? query.OrderBy(t => ratingSortOrders.GetValueOrDefault(t.RatingId, 0))
                : query.OrderByDescending(t => ratingSortOrders.GetValueOrDefault(t.RatingId, 0)),
            TrackSortField.DownloadedAt => sortDirection == TrackSortDirection.Ascending
                ? query.OrderBy(t => t.DownloadedAt)
                : query.OrderByDescending(t => t.DownloadedAt),
            TrackSortField.Duration => sortDirection == TrackSortDirection.Ascending
                ? query.OrderBy(t => t.DurationSeconds ?? int.MaxValue)
                : query.OrderByDescending(t => t.DurationSeconds ?? -1),
            _ => query.OrderByDescending(t => t.DownloadedAt),
        };

        return sorted.ToList();
    }

    // A track matches a group if it has ALL the group's genres AND ALL the group's styles.
    private static bool MatchesGroup(
        MusicTrack track,
        FilterGroup group,
        IReadOnlyDictionary<int, List<int>> trackGenreIds,
        IReadOnlyDictionary<int, List<int>> trackStyleIds)
    {
        if (group.GenreIds.Count > 0)
        {
            trackGenreIds.TryGetValue(track.Id, out var tGenres);
            tGenres ??= [];
            if (!group.GenreIds.All(id => tGenres.Contains(id))) return false;
        }

        if (group.StyleIds.Count > 0)
        {
            trackStyleIds.TryGetValue(track.Id, out var tStyles);
            tStyles ??= [];
            if (!group.StyleIds.All(id => tStyles.Contains(id))) return false;
        }

        return true;
    }

    private static bool MatchesSearch(MusicTrack t, string term)
    {
        if (t.Title.Contains(term, StringComparison.OrdinalIgnoreCase)) return true;
        if (t.Notes != null && t.Notes.Contains(term, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
