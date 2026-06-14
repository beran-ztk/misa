using System.Collections.Generic;
using Misa.Music.Models;

namespace Misa.Models;

public record TrackDisplayItem(MusicTrack Track, string MetaLine, List<int> StyleIds)
{
    public string Title => Track.Title;
    public int GenreId => Track.GenreId;
    public int RatingId => Track.RatingId;
    public bool IsPlaying { get; set; }
}
