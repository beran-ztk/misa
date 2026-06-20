using Avalonia.Media.Imaging;
using Avalonia.Media;

namespace Music.Models;

public record TrackDisplayItem(
    MusicTrack Track,
    string SystemGenreText,
    string ManualGenreText,
    string StyleText,
    string DurationText,
    string RatingText,
    string ProfileText,
    string ChannelText)
{
    public bool IsPlaying { get; set; }
    public bool NeedsReview { get; set; }
    public Bitmap? Thumbnail { get; set; }

    public IBrush PlayingBackground => IsPlaying
        ? new SolidColorBrush(Color.FromArgb(48, 28, 132, 184))
        : Brushes.Transparent;

    public IBrush PlayingAccent => IsPlaying
        ? new SolidColorBrush(Color.FromRgb(39, 172, 231))
        : Brushes.Transparent;

    public IBrush TitleBrush => IsPlaying
        ? new SolidColorBrush(Color.FromRgb(246, 251, 255))
        : new SolidColorBrush(Color.FromRgb(238, 241, 245));

    public IBrush RatingBackground => RatingText switch
    {
        "Favorite" => new SolidColorBrush(Color.FromArgb(42, 255, 202, 91)),
        "Great" => new SolidColorBrush(Color.FromArgb(38, 72, 194, 120)),
        "Good" => new SolidColorBrush(Color.FromArgb(36, 58, 151, 214)),
        "Okay" => new SolidColorBrush(Color.FromArgb(32, 156, 166, 179)),
        "Skip" => new SolidColorBrush(Color.FromArgb(35, 224, 92, 92)),
        _ => new SolidColorBrush(Color.FromArgb(24, 156, 166, 179))
    };

    public IBrush RatingBorder => RatingText switch
    {
        "Favorite" => new SolidColorBrush(Color.FromArgb(105, 255, 202, 91)),
        "Great" => new SolidColorBrush(Color.FromArgb(100, 72, 194, 120)),
        "Good" => new SolidColorBrush(Color.FromArgb(95, 58, 151, 214)),
        "Okay" => new SolidColorBrush(Color.FromArgb(80, 156, 166, 179)),
        "Skip" => new SolidColorBrush(Color.FromArgb(95, 224, 92, 92)),
        _ => new SolidColorBrush(Color.FromArgb(65, 156, 166, 179))
    };
}
