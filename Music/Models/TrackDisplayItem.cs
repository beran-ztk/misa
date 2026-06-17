using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace Music.Models;

public record TrackDisplayItem(
    MusicTrack Track,
    string MetaLine,
    List<int> GenreIds,
    List<int> StyleIds,
    string GenreText,
    string StyleText,
    string DurationText,
    string RatingText)
{
    public string Title => Track.Title;
    public bool IsPlaying { get; set; }
    public Bitmap? Thumbnail { get; set; }
}
