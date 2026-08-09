using System;

namespace Music.Models;

public sealed class AppearanceSettings
{
    public double PlayerArtworkStrength { get; set; } = 64.5;
    public double PlayerArtworkBlur { get; set; } = 50;
    public double PlayerBackgroundDarkening { get; set; } = 4.6;
    public double PlayerColorAtmosphere { get; set; } = 19.5;
    public double PlayerAudioReaction { get; set; } = 65;
    public double AudioResponseSpeed { get; set; } = 100;
    public double AudioBassSensitivity { get; set; } = 175;
    public double AudioTrebleSensitivity { get; set; } = 125;
    public double AudioArtworkMotion { get; set; } = 150;
    public double AudioBlurReaction { get; set; } = 75;
    public double AudioColorReaction { get; set; } = 200;
    public bool SpectrumVisualizerEnabled { get; set; } = true;
    public double SpectrumVisualizerHeight { get; set; } = 140;
    // Kept nullable for one load cycle so settings written by the first visualizer
    // version can be migrated. The visualizer now renders without global opacity.
    public double? SpectrumVisualizerOpacity { get; set; }
    public double SpectrumVisualizerSensitivity { get; set; } = 100;
    public double SpectrumVisualizerSmoothing { get; set; } = 65;
    public double LibraryBackdropStrength { get; set; } = 14.6;
    public double LibraryBackdropBlur { get; set; } = 0;
    public double TrackArtworkStrength { get; set; } = 35.25;
    public double TrackArtworkBlur { get; set; } = 20.4;
    public double TrackColorWashStrength { get; set; } = 25.35;
    public double TrackColorWashReach { get; set; } = 40;
    public double CoverHaloStrength { get; set; } = 60;
    public double CoverHaloBlur { get; set; } = 20;

    public AppearanceSettings Clone() => new()
    {
        PlayerArtworkStrength = PlayerArtworkStrength,
        PlayerArtworkBlur = PlayerArtworkBlur,
        PlayerBackgroundDarkening = PlayerBackgroundDarkening,
        PlayerColorAtmosphere = PlayerColorAtmosphere,
        PlayerAudioReaction = PlayerAudioReaction,
        AudioResponseSpeed = AudioResponseSpeed,
        AudioBassSensitivity = AudioBassSensitivity,
        AudioTrebleSensitivity = AudioTrebleSensitivity,
        AudioArtworkMotion = AudioArtworkMotion,
        AudioBlurReaction = AudioBlurReaction,
        AudioColorReaction = AudioColorReaction,
        SpectrumVisualizerEnabled = SpectrumVisualizerEnabled,
        SpectrumVisualizerHeight = SpectrumVisualizerHeight,
        SpectrumVisualizerOpacity = SpectrumVisualizerOpacity,
        SpectrumVisualizerSensitivity = SpectrumVisualizerSensitivity,
        SpectrumVisualizerSmoothing = SpectrumVisualizerSmoothing,
        LibraryBackdropStrength = LibraryBackdropStrength,
        LibraryBackdropBlur = LibraryBackdropBlur,
        TrackArtworkStrength = TrackArtworkStrength,
        TrackArtworkBlur = TrackArtworkBlur,
        TrackColorWashStrength = TrackColorWashStrength,
        TrackColorWashReach = TrackColorWashReach,
        CoverHaloStrength = CoverHaloStrength,
        CoverHaloBlur = CoverHaloBlur
    };

    public AppearanceSettings Clamp()
    {
        PlayerArtworkStrength = Math.Clamp(PlayerArtworkStrength, 0, 100);
        PlayerArtworkBlur = Math.Clamp(PlayerArtworkBlur, 0, 50);
        PlayerBackgroundDarkening = Math.Clamp(PlayerBackgroundDarkening, 0, 80);
        PlayerColorAtmosphere = Math.Clamp(PlayerColorAtmosphere, 0, 100);
        PlayerAudioReaction = Math.Clamp(PlayerAudioReaction, 0, 100);
        AudioResponseSpeed = Math.Clamp(AudioResponseSpeed, 0, 100);
        AudioBassSensitivity = Math.Clamp(AudioBassSensitivity, 0, 200);
        AudioTrebleSensitivity = Math.Clamp(AudioTrebleSensitivity, 0, 200);
        AudioArtworkMotion = Math.Clamp(AudioArtworkMotion, 0, 200);
        AudioBlurReaction = Math.Clamp(AudioBlurReaction, 0, 200);
        AudioColorReaction = Math.Clamp(AudioColorReaction, 0, 200);
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
        SpectrumVisualizerSensitivity = Math.Clamp(SpectrumVisualizerSensitivity, 25, 250);
        SpectrumVisualizerSmoothing = Math.Clamp(SpectrumVisualizerSmoothing, 0, 95);
        LibraryBackdropStrength = Math.Clamp(LibraryBackdropStrength, 0, 60);
        LibraryBackdropBlur = Math.Clamp(LibraryBackdropBlur, 0, 50);
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
        PlayerArtworkStrength = 40,
        PlayerArtworkBlur = 34,
        PlayerBackgroundDarkening = 64,
        PlayerColorAtmosphere = 58,
        PlayerAudioReaction = 45,
        AudioResponseSpeed = 38,
        AudioBassSensitivity = 80,
        AudioTrebleSensitivity = 75,
        AudioArtworkMotion = 55,
        AudioBlurReaction = 60,
        AudioColorReaction = 55,
        SpectrumVisualizerHeight = 100,
        SpectrumVisualizerSensitivity = 85,
        SpectrumVisualizerSmoothing = 78,
        LibraryBackdropStrength = 16,
        LibraryBackdropBlur = 25,
        TrackArtworkStrength = 11,
        TrackArtworkBlur = 17,
        TrackColorWashStrength = 8,
        TrackColorWashReach = 76,
        CoverHaloStrength = 20,
        CoverHaloBlur = 11
    };

    public static AppearanceSettings Vibrant() => new()
    {
        PlayerArtworkStrength = 70,
        PlayerArtworkBlur = 24,
        PlayerBackgroundDarkening = 44,
        PlayerColorAtmosphere = 100,
        PlayerAudioReaction = 100,
        AudioResponseSpeed = 68,
        AudioBassSensitivity = 125,
        AudioTrebleSensitivity = 115,
        AudioArtworkMotion = 135,
        AudioBlurReaction = 125,
        AudioColorReaction = 135,
        SpectrumVisualizerHeight = 180,
        SpectrumVisualizerSensitivity = 125,
        SpectrumVisualizerSmoothing = 48,
        LibraryBackdropStrength = 36,
        LibraryBackdropBlur = 17,
        TrackArtworkStrength = 28,
        TrackArtworkBlur = 11,
        TrackColorWashStrength = 21,
        TrackColorWashReach = 100,
        CoverHaloStrength = 48,
        CoverHaloBlur = 7
    };
}
