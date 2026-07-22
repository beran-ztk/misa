using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Avalonia.Media;

namespace Music.Models;

public record TrackTagDisplay(string Name, IBrush Foreground);

public record TrackDisplayItem(
    MusicTrack Track,
    string GenreText,
    string ModelGenreText,
    string ManualGenreText,
    string StyleText,
    string DurationText,
    string RatingText,
    IReadOnlyList<TrackTagDisplay> TagDisplays,
    string ChannelText)
{
    public bool IsPlaying { get; set; }
    public bool NeedsReview { get; set; }
    public bool NeedsAnalysis { get; set; }
    public Bitmap? Thumbnail { get; set; }

    public string ChannelSeparatorText => string.IsNullOrWhiteSpace(ChannelText)
                                                  || (string.IsNullOrWhiteSpace(ModelGenreText)
                                                      && string.IsNullOrWhiteSpace(ManualGenreText))
        ? string.Empty
        : "  ·  ";

    public string GenreSeparatorText => string.IsNullOrWhiteSpace(ModelGenreText) || string.IsNullOrWhiteSpace(ManualGenreText)
        ? string.Empty
        : "  ·  ";

    public IBrush PlayingBackground => IsPlaying
        ? new SolidColorBrush(Color.FromArgb(40, 188, 174, 127))
        : Brushes.Transparent;

    public IBrush PlayingAccent => IsPlaying
        ? new SolidColorBrush(Color.FromRgb(183, 240, 0))
        : Brushes.Transparent;

    public IBrush TitleBrush => NeedsAnalysis
        ? new SolidColorBrush(Color.FromRgb(238, 92, 92))
        : NeedsReview
        ? new SolidColorBrush(Color.FromRgb(255, 210, 122))
        : IsPlaying
            ? new SolidColorBrush(Color.FromRgb(247, 246, 236))
        : new SolidColorBrush(Color.FromRgb(240, 239, 229));

    public IBrush RatingBackground => RatingText switch
    {
        "Favorite" => new SolidColorBrush(Color.FromArgb(70, 132, 105, 36)),
        "Great" => new SolidColorBrush(Color.FromArgb(68, 35, 105, 58)),
        "Good" => new SolidColorBrush(Color.FromArgb(66, 31, 93, 82)),
        "Okay" => new SolidColorBrush(Color.FromArgb(62, 83, 84, 64)),
        RatingNames.Avoid => new SolidColorBrush(Color.FromArgb(66, 124, 47, 40)),
        _ => new SolidColorBrush(Color.FromArgb(60, 103, 76, 42))
    };

    public IBrush RatingBorder => RatingText switch
    {
        "Favorite" => new SolidColorBrush(Color.FromArgb(185, 219, 184, 85)),
        "Great" => new SolidColorBrush(Color.FromArgb(175, 83, 176, 105)),
        "Good" => new SolidColorBrush(Color.FromArgb(170, 76, 164, 139)),
        "Okay" => new SolidColorBrush(Color.FromArgb(150, 139, 144, 108)),
        RatingNames.Avoid => new SolidColorBrush(Color.FromArgb(170, 201, 82, 68)),
        _ => new SolidColorBrush(Color.FromArgb(160, 190, 139, 69))
    };

    public IBrush RatingForeground => RatingText switch
    {
        "Favorite" => new SolidColorBrush(Color.FromRgb(255, 230, 150)),
        "Great" => new SolidColorBrush(Color.FromRgb(188, 242, 185)),
        "Good" => new SolidColorBrush(Color.FromRgb(176, 232, 212)),
        "Okay" => new SolidColorBrush(Color.FromRgb(226, 224, 194)),
        RatingNames.Avoid => new SolidColorBrush(Color.FromRgb(246, 175, 160)),
        _ => new SolidColorBrush(Color.FromRgb(243, 203, 128))
    };
}
