using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace Music.Models;

public record TrackDisplayItem(
    MusicTrack Track,
    string MetaLine,
    List<int> GenreIds,
    List<int> StyleIds,
    List<int> LanguageIds,
    string GenreText,
    string StyleText,
    string DurationText,
    string RatingText)
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
    public string ListenText => $"▶ {Track.ListenCount}";
    public string SkipText => $"↷ {Track.SkipCount}";
    public bool IsPlaying { get; set; }
    public Bitmap? Thumbnail { get; set; }

    public bool HasGenres => GenreText.Length > 0;
    public bool HasStyles => StyleText.Length > 0;
    public bool HasDuration => DurationText.Length > 0;
    public bool HasRating => RatingText.Length > 0;
    public bool HasMetaLine => MetaLine.Length > 0;
}
