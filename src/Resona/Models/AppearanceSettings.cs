using System;

namespace Resona.Models;

public sealed class AppearanceSettings
{
    public double SongFadeDuration { get; set; } = 10;
    public double TrackArtworkStrength { get; set; } = 30.25;
    public double TrackArtworkBlur { get; set; } = 30;
    public double TrackColorWashStrength { get; set; } = 30;
    public double TrackColorWashReach { get; set; } = 44.8;
    public double CoverHaloStrength { get; set; } = 60;
    public double CoverHaloBlur { get; set; } = 20;

    public AppearanceSettings Clone() => new()
    {
        SongFadeDuration = SongFadeDuration,
        TrackArtworkStrength = TrackArtworkStrength,
        TrackArtworkBlur = TrackArtworkBlur,
        TrackColorWashStrength = TrackColorWashStrength,
        TrackColorWashReach = TrackColorWashReach,
        CoverHaloStrength = CoverHaloStrength,
        CoverHaloBlur = CoverHaloBlur
    };

    public AppearanceSettings Clamp()
    {
        SongFadeDuration = Math.Clamp(SongFadeDuration, 0, 30);
        TrackArtworkStrength = Math.Clamp(TrackArtworkStrength, 0, 50);
        TrackArtworkBlur = Math.Clamp(TrackArtworkBlur, 0, 30);
        TrackColorWashStrength = Math.Clamp(TrackColorWashStrength, 0, 30);
        TrackColorWashReach = Math.Clamp(TrackColorWashReach, 20, 100);
        CoverHaloStrength = Math.Clamp(CoverHaloStrength, 0, 60);
        CoverHaloBlur = Math.Clamp(CoverHaloBlur, 0, 20);
        return this;
    }

    public static AppearanceSettings Balanced() => new();

    public static AppearanceSettings Subtle() => new()
    {
        TrackArtworkStrength = 11,
        TrackArtworkBlur = 17,
        TrackColorWashStrength = 8,
        TrackColorWashReach = 76,
        CoverHaloStrength = 20,
        CoverHaloBlur = 11
    };

    public static AppearanceSettings Vibrant() => new()
    {
        TrackArtworkStrength = 28,
        TrackArtworkBlur = 11,
        TrackColorWashStrength = 21,
        TrackColorWashReach = 100,
        CoverHaloStrength = 48,
        CoverHaloBlur = 7
    };

}
