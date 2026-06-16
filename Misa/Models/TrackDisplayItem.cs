using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Misa.Music.Models;

namespace Misa.Models;

public record TrackDisplayItem(MusicTrack Track, string MetaLine, List<int> GenreIds, List<int> StyleIds, List<int> LanguageIds)
{
    public string Title => Track.Title;
    public int RatingId => Track.RatingId;
    public string? Notes => Track.Notes;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Track.Notes);
    public bool ReEvaluationNeeded => Track.ReEvaluationNeeded;
    public int ListenCount => Track.ListenCount;
    public int SkipCount => Track.SkipCount;
    public bool HasStats => Track.ListenCount > 0 || Track.SkipCount > 0;
    public string StatsLine => $"▶ {Track.ListenCount}  ⏭ {Track.SkipCount}";
    public bool IsPlaying { get; set; }
    public Bitmap? Thumbnail { get; set; }
}
