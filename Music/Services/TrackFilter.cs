using System;
using System.Collections.Generic;
using System.Linq;
using Music.Models;

namespace Music.Services;

public record FilterGroup(IReadOnlySet<int> GenreIds, IReadOnlySet<int> StyleIds, IReadOnlySet<int> TagIds, bool Negate = false);

public static class TrackFilter
{
    public static List<MusicTrack> Apply(
        IEnumerable<MusicTrack> tracks,
        IReadOnlyDictionary<int, List<int>> trackGenreIds,
        IReadOnlyDictionary<int, List<int>> trackStyleIds,
        IReadOnlyDictionary<int, List<int>> trackTagIds,
        IReadOnlySet<int> ratingFilter,
        IReadOnlySet<bool> visibilityFilter,
        IReadOnlyList<FilterGroup> filterGroups,
        string? searchText)
    {
        IEnumerable<MusicTrack> query = tracks;

        if (ratingFilter.Count > 0)
            query = query.Where(t => t.RatingId is int ratingId && ratingFilter.Contains(ratingId));

        if (visibilityFilter.Count > 0)
            query = query.Where(t => visibilityFilter.Contains(t.IsPublic));
        

        var term = searchText?.Trim();
        if (!string.IsNullOrEmpty(term))
            query = query.Where(t => t.Title.Contains(term, StringComparison.OrdinalIgnoreCase));
        
        // Apply filter groups: OR between positive groups, AND within a group.
        // Negated groups remove matching tracks after the positive groups are evaluated.
        // Empty groups (nothing selected in any dimension) are ignored.
        var activeGroups = filterGroups
            .Where(g => g.GenreIds.Count > 0 || g.StyleIds.Count > 0 || g.TagIds.Count > 0)
            .ToList();
        var includeGroups = activeGroups.Where(group => !group.Negate).ToList();
        var excludeGroups = activeGroups.Where(group => group.Negate).ToList();

        if (includeGroups.Count > 0)
            query = query.Where(track => includeGroups.Any(g =>
                MatchesGroup(track, g, trackGenreIds, trackStyleIds, trackTagIds)));

        if (excludeGroups.Count > 0)
            query = query.Where(track => !excludeGroups.Any(g =>
                MatchesGroup(track, g, trackGenreIds, trackStyleIds, trackTagIds)));

        var sorted = query.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase);

        return sorted.ToList();
    }

    // A track matches a group if it has all selected genres and all selected styles.
    private static bool MatchesGroup(
        MusicTrack track,
        FilterGroup group,
        IReadOnlyDictionary<int, List<int>> trackGenreIds,
        IReadOnlyDictionary<int, List<int>> trackStyleIds,
        IReadOnlyDictionary<int, List<int>> trackTagIds)
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

        if (group.TagIds.Count > 0)
        {
            trackTagIds.TryGetValue(track.Id, out var tTags);
            tTags ??= [];
            if (!group.TagIds.All(id => tTags.Contains(id))) return false;
        }

        return true;
    }
}
