namespace Resona.Models;

public enum MusicVideoImageMode
{
    Fit,
    Crop,
    BlurredBackground
}

public enum MusicVideoAnimation
{
    None,
    ZoomIn,
    ZoomOut,
    Pan
}

public enum MusicVideoAnimationDirection
{
    Left,
    Right,
    Up,
    Down
}

public sealed record MusicVideoOptions
{
    public required string AudioPath { get; init; }
    public required string ImagePath { get; init; }
    public required string OutputPath { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public int Width { get; init; } = 1920;
    public int Height { get; init; } = 1080;
    public MusicVideoImageMode ImageMode { get; init; } = MusicVideoImageMode.Fit;
    public MusicVideoAnimation Animation { get; init; }
    public MusicVideoAnimationDirection AnimationDirection { get; init; } = MusicVideoAnimationDirection.Right;
    public double AnimationStrength { get; init; } = 0.35;
    public double BackgroundBlur { get; init; } = 30;
    public double BackgroundDim { get; init; } = 0.18;
    public double ImageScale { get; init; } = 1;
    public double ImagePositionX { get; init; }
    public double ImagePositionY { get; init; }
    public double TextPositionX { get; init; } = 0.5;
    public double TextPositionY { get; init; } = 0.78;
}

public sealed record MusicVideoProgress(double Fraction, string Stage);
