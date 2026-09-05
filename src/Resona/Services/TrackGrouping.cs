using System.Collections.Generic;
using System.Linq;
using Resona.Models;

namespace Resona.Services;

public sealed record TrackGroupRow(MusicTrack Track, bool IsContextOnly, bool IsChild);

public static class TrackGrouping
{
    // Only the supplied matches belong in playback. Added parents are display context.
    // First matching member determines group order; callers can pre-sort by the root title.
    public static List<TrackGroupRow> Build(IReadOnlyList<MusicTrack> matches, IEnumerable<MusicTrack> allTracks)
    {
        var all = allTracks.ToDictionary(track => track.Id);
        var matchIds = matches.Select(track => track.Id).ToHashSet();
        int Root(MusicTrack track) => !track.IsOriginal && track.ParentTrackId is int parent
            && all.TryGetValue(parent, out var original) && original.IsOriginal ? parent : track.Id;
        var result = new List<TrackGroupRow>();
        foreach (var group in matches.GroupBy(Root))
        {
            var root = all.GetValueOrDefault(group.Key) ?? group.First();
            result.Add(new(root, !matchIds.Contains(root.Id), false));
            result.AddRange(group.Where(track => track.Id != root.Id)
                .Select(track => new TrackGroupRow(track, false, true)));
        }
        return result;
    }
}
