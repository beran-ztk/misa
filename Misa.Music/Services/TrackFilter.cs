using System;
using System.Collections.Generic;
using System.Linq;
using Misa.Music.Models;

namespace Misa.Music.Services;

public enum TrackSortField { Title, Rating, DownloadedAt, Duration }
public enum TrackSortDirection { Ascending, Descending }

public static class TrackFilter
{
    public static List<MusicTrack> Apply(
        IEnumerable<MusicTrack> tracks,
        IReadOnlyDictionary<int, List<int>> trackStyleIds,
        IReadOnlyDictionary<int, int> ratingSortOrders,
        IReadOnlySet<int> genreFilter,
        IReadOnlySet<int> ratingFilter,
        IReadOnlySet<int> styleFilter,
        string? searchText,
        TrackSortField sortField,
        TrackSortDirection sortDirection)
    {
        IEnumerable<MusicTrack> query = tracks;

        if (genreFilter.Count > 0)
            query = query.Where(t => genreFilter.Contains(t.GenreId));

        if (ratingFilter.Count > 0)
            query = query.Where(t => ratingFilter.Contains(t.RatingId));

        if (styleFilter.Count > 0)
            query = query.Where(t =>
                trackStyleIds.TryGetValue(t.Id, out var ids) &&
                ids.Any(id => styleFilter.Contains(id)));

        var term = searchText?.Trim();
        if (!string.IsNullOrEmpty(term))
            query = query.Where(t => MatchesSearch(t, term));

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

    private static bool MatchesSearch(MusicTrack t, string term)
    {
        if (t.Title.Contains(term, StringComparison.OrdinalIgnoreCase)) return true;
        if (t.Notes != null && t.Notes.Contains(term, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
