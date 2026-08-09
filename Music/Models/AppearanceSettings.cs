using System;

namespace Music.Models;

public sealed class AppearanceSettings
{
    public double PlayerArtworkStrength { get; set; } = 55;
    public double PlayerArtworkBlur { get; set; } = 30;
    public double PlayerBackgroundDarkening { get; set; } = 56;
    public double PlayerColorAtmosphere { get; set; } = 100;
    public double PlayerAudioReaction { get; set; } = 100;
    public double AudioResponseSpeed { get; set; } = 50;
    public double AudioBassSensitivity { get; set; } = 100;
    public double AudioTrebleSensitivity { get; set; } = 100;
    public double AudioArtworkMotion { get; set; } = 100;
    public double AudioBlurReaction { get; set; } = 100;
    public double AudioColorReaction { get; set; } = 100;
    public double LibraryBackdropStrength { get; set; } = 25;
    public double LibraryBackdropBlur { get; set; } = 20;
    public double TrackArtworkStrength { get; set; } = 18;
    public double TrackArtworkBlur { get; set; } = 14;
    public double TrackColorWashStrength { get; set; } = 13;
    public double TrackColorWashReach { get; set; } = 88;
    public double CoverHaloStrength { get; set; } = 34;
    public double CoverHaloBlur { get; set; } = 9;

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
