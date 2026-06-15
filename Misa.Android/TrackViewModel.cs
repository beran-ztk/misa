using Misa.Shared.Models;

namespace Misa.Android;

public class TrackViewModel
{
    public LibraryTrack Track { get; }
    public string Title => Track.Title;
    public string MetaLine { get; }
    public string DurationText { get; }

    public TrackViewModel(LibraryTrack track)
    {
        Track = track;

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(track.Genre)) parts.Add(track.Genre);
        if (!string.IsNullOrEmpty(track.Rating)) parts.Add(track.Rating);
        if (track.Styles.Count > 0) parts.Add(string.Join(", ", track.Styles));
        MetaLine = string.Join(" · ", parts);

        DurationText = track.DurationSeconds is int s
            ? $"{s / 60}:{s % 60:D2}"
            : "";
    }
}
