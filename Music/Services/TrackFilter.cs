using System;
using System.Collections.Generic;
using System.Linq;
using Music.Models;

namespace Music.Services;

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
        string? searchText)
    {
        IEnumerable<MusicTrack> query = tracks;

        if (ratingFilter.Count > 0)
            query = query.Where(t => ratingFilter.Contains(t.RatingId));
        

        var term = searchText?.Trim();
        if (!string.IsNullOrEmpty(term))
            query = query.Where(t => t.Title.Contains(term, StringComparison.OrdinalIgnoreCase));
        
        // Apply filter groups: OR between groups, AND within a group.
        // Empty groups (nothing selected in any dimension) are ignored.
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

        var sorted = query.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase);

        return sorted.ToList();
    }

    // A track matches a group if it has ALL the group's genres, ALL the group's styles,
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
}
