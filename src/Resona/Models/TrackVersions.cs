using System;
using System.Collections.Generic;
using System.Linq;

namespace Resona.Models;

[Flags]
public enum TrackEditTypes
{
    None = 0,
    SpeedUp = 1,
    Nightcore = 2,
    Slowed = 4,
    Reverb = 8,
    Remix = 16
}

public static class TrackVersions
{
    public static readonly (TrackEditTypes Type, string Name)[] Types =
    [
        (TrackEditTypes.SpeedUp, "Speed Up"),
        (TrackEditTypes.Nightcore, "Nightcore"),
        (TrackEditTypes.Slowed, "Slowed"),
        (TrackEditTypes.Reverb, "Reverb"),
        (TrackEditTypes.Remix, "Remix")
    ];

    public static string Label(MusicTrack track) => track.IsOriginal
        ? "Original"
        : track.EditTypes == TrackEditTypes.None ? "Edit"
        : string.Join(" · ", Types.Where(t => track.EditTypes.HasFlag(t.Type)).Select(t => t.Name));

    public static bool Matches(MusicTrack track, IEnumerable<string> versions) => versions.Any(name =>
        name == "Original" ? track.IsOriginal : name == "Edit" ? !track.IsOriginal
        : !track.IsOriginal && Types.Any(t => t.Name == name && track.EditTypes.HasFlag(t.Type)));
}
