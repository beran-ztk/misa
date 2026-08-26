using System;

namespace Resona.Models;

public sealed class AppearanceSettings
{
    public double SongFadeDuration { get; set; } = 10;
    public bool SpectrumVisualizerEnabled { get; set; } = true;
    public double SpectrumVisualizerHeight { get; set; } = 139.9;
    // Legacy field kept temporarily so settings written by the first visualizer
    // version can be migrated. Visual strength now uses SpectrumVisualizerIntensity.
    public double? SpectrumVisualizerOpacity { get; set; }
    public double SpectrumVisualizerIntensity { get; set; } = 50;
    public double SpectrumVisualizerSensitivity { get; set; } = 108.25;
    public double SpectrumVisualizerSmoothing { get; set; } = 64.7;
    public double TrackArtworkStrength { get; set; } = 30.25;
    public double TrackArtworkBlur { get; set; } = 30;
    public double TrackColorWashStrength { get; set; } = 30;
    public double TrackColorWashReach { get; set; } = 44.8;
    public double CoverHaloStrength { get; set; } = 60;
    public double CoverHaloBlur { get; set; } = 20;

    public AppearanceSettings Clone() => new()
    {
        SongFadeDuration = SongFadeDuration,
        SpectrumVisualizerEnabled = SpectrumVisualizerEnabled,
        SpectrumVisualizerHeight = SpectrumVisualizerHeight,
        SpectrumVisualizerOpacity = SpectrumVisualizerOpacity,
        SpectrumVisualizerIntensity = SpectrumVisualizerIntensity,
        SpectrumVisualizerSensitivity = SpectrumVisualizerSensitivity,
        SpectrumVisualizerSmoothing = SpectrumVisualizerSmoothing,
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
        if (SpectrumVisualizerOpacity is not null)
        {
            if (Math.Abs(SpectrumVisualizerHeight - 100) < 0.01
                && SpectrumVisualizerOpacity <= 22.01)
            {
                SpectrumVisualizerHeight = 140;
            }
            SpectrumVisualizerOpacity = null;
        }
        SpectrumVisualizerHeight = Math.Clamp(SpectrumVisualizerHeight, 40, 220);
        SpectrumVisualizerIntensity = Math.Clamp(SpectrumVisualizerIntensity, 0, 100);
        SpectrumVisualizerSensitivity = Math.Clamp(SpectrumVisualizerSensitivity, 25, 250);
        SpectrumVisualizerSmoothing = Math.Clamp(SpectrumVisualizerSmoothing, 0, 95);
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
        SpectrumVisualizerHeight = 100,
        SpectrumVisualizerSensitivity = 85,
        SpectrumVisualizerSmoothing = 78,
        TrackArtworkStrength = 11,
        TrackArtworkBlur = 17,
        TrackColorWashStrength = 8,
        TrackColorWashReach = 76,
        CoverHaloStrength = 20,
        CoverHaloBlur = 11
    };

    public static AppearanceSettings Vibrant() => new()
    {
        SpectrumVisualizerHeight = 180,
        SpectrumVisualizerSensitivity = 125,
        SpectrumVisualizerSmoothing = 48,
        TrackArtworkStrength = 28,
        TrackArtworkBlur = 11,
        TrackColorWashStrength = 21,
        TrackColorWashReach = 100,
        CoverHaloStrength = 48,
        CoverHaloBlur = 7
    };

}
