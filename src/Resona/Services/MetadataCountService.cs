using System.Collections.Generic;
using System.Linq;

namespace Resona.Services;

public static class MetadataCountService
{
    // Count how many tracks use each genre id across the entire library.
    public static Dictionary<int, int> GenreCounts(IReadOnlyDictionary<int, List<int>> trackGenreIds)
    {
        var counts = new Dictionary<int, int>();
        foreach (var (_, genreIds) in trackGenreIds)
            foreach (var gid in genreIds)
                counts[gid] = counts.GetValueOrDefault(gid, 0) + 1;
        return counts;
    }

    // Count how many tracks that have ALL of selectedGenreIds also use each style id.
    // When selectedGenreIds is empty, counts across all tracks.
    public static Dictionary<int, int> StyleCountsForGenres(
        IReadOnlyDictionary<int, List<int>> trackGenreIds,
        IReadOnlyDictionary<int, List<int>> trackStyleIds,
        IReadOnlySet<int> selectedGenreIds)
    {
        var counts = new Dictionary<int, int>();
        foreach (var (trackId, styleIds) in trackStyleIds)
        {
            if (selectedGenreIds.Count > 0)
            {
                trackGenreIds.TryGetValue(trackId, out var tGenres);
                tGenres ??= [];
                if (!selectedGenreIds.All(gid => tGenres.Contains(gid))) continue;
            }
            foreach (var sid in styleIds)
                counts[sid] = counts.GetValueOrDefault(sid, 0) + 1;
        }
        return counts;
    }

    // Faceted filter counts: for each tag id, count how many of the current result tracks also
    // have that tag. This gives:
    //   - Selected tags: count == current result count (all results have all selected tags).
    //   - Unselected tags: count == how many results would remain if that tag were added (AND).
    public static Dictionary<int, int> FacetCounts(
        IReadOnlyList<int> currentResultTrackIds,
        IReadOnlyDictionary<int, List<int>> trackTagIds)
    {
        var counts = new Dictionary<int, int>();
        foreach (var trackId in currentResultTrackIds)
        {
            if (!trackTagIds.TryGetValue(trackId, out var tagIds)) continue;
            foreach (var tid in tagIds)
                counts[tid] = counts.GetValueOrDefault(tid, 0) + 1;
        }
        return counts;
    }
}
