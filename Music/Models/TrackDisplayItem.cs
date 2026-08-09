using System;
using System.Collections.Generic;
using Avalonia;
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
    public Color ArtworkPrimaryColor { get; private set; } = Colors.Transparent;
    public Color ArtworkSecondaryColor { get; private set; } = Colors.Transparent;
    public IBrush ArtworkRowBackground { get; private set; } = Brushes.Transparent;
    public IBrush ArtworkMetadataBrush { get; private set; } = new SolidColorBrush(Color.Parse("#D4CFB4"));
    public IBrush ArtworkBorderBrush { get; private set; } = new SolidColorBrush(Color.Parse("#46514D40"));
    public double TrackArtworkOpacity { get; private set; } = 0.18;
    public double TrackArtworkBlur { get; private set; } = 14;
    public double CoverHaloOpacity { get; private set; } = 0.34;
    public double CoverHaloBlur { get; private set; } = 9;

    private AppearanceSettings _appearance = AppearanceSettings.Balanced();

    public void SetArtworkPalette(Color primary, Color secondary)
    {
        ArtworkPrimaryColor = Color.FromRgb(primary.R, primary.G, primary.B);
        ArtworkSecondaryColor = Color.FromRgb(secondary.R, secondary.G, secondary.B);
        RebuildArtworkPresentation();
    }

    public void ApplyAppearance(AppearanceSettings appearance)
    {
        _appearance = appearance;
        TrackArtworkOpacity = _appearance.TrackArtworkStrength / 100d;
        TrackArtworkBlur = _appearance.TrackArtworkBlur;
        CoverHaloOpacity = _appearance.CoverHaloStrength / 100d;
        CoverHaloBlur = _appearance.CoverHaloBlur;
        RebuildArtworkPresentation();
    }

    private void RebuildArtworkPresentation()
    {
        if (ArtworkPrimaryColor.A == 0)
        {
            ArtworkRowBackground = Brushes.Transparent;
            return;
        }

        var strength = _appearance.TrackColorWashStrength / 100d;
        ArtworkRowBackground = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(_appearance.TrackColorWashReach / 100d, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(WithOpacity(ArtworkPrimaryColor, strength), 0),
                new GradientStop(WithOpacity(ArtworkSecondaryColor, strength * 0.6), 0.42),
                new GradientStop(Colors.Transparent, 1)
            }
        };
        ArtworkMetadataBrush = new SolidColorBrush(Mix(ArtworkPrimaryColor, Color.Parse("#E2DDCA"), 0.58));
        ArtworkBorderBrush = new SolidColorBrush(Color.FromArgb(68, ArtworkPrimaryColor.R, ArtworkPrimaryColor.G, ArtworkPrimaryColor.B));
    }

    private static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)Math.Round(opacity * 255), 0, 255), color.R, color.G, color.B);

    private static Color Mix(Color from, Color to, double amount) => Color.FromRgb(
        (byte)(from.R + (to.R - from.R) * amount),
        (byte)(from.G + (to.G - from.G) * amount),
        (byte)(from.B + (to.B - from.B) * amount));

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
