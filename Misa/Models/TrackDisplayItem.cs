using System.Collections.Generic;
using Misa.Music.Models;

namespace Misa.Models;

public record TrackDisplayItem(MusicTrack Track, string MetaLine, List<int> GenreIds, List<int> StyleIds, List<int> LanguageIds)
{
    public string Title => Track.Title;
    public int RatingId => Track.RatingId;
    public string? Notes => Track.Notes;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Track.Notes);
    public bool ReEvaluationNeeded => Track.ReEvaluationNeeded;
    public bool IsPlaying { get; set; }
}
