using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Music.Models;
using Music.Services;

namespace Music.ViewModels;

public sealed class MusicVideoViewModel : INotifyPropertyChanged
{
    private readonly IMusicVideoService _service;
    private CancellationTokenSource? _exportCancellation;
    private string _audioPath = string.Empty;
    private string _imagePath = string.Empty;
    private string _outputPath = string.Empty;
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private int _width = 1920;
    private int _height = 1080;
    private MusicVideoImageMode _imageMode = MusicVideoImageMode.Fit;
    private MusicVideoAnimation _animation;
    private MusicVideoAnimationDirection _animationDirection = MusicVideoAnimationDirection.Right;
    private double _animationStrength = 0.35;
    private double _backgroundBlur = 30;
    private double _backgroundDim = 0.18;
    private double _imageScale = 1;
    private double _imagePositionX;
    private double _imagePositionY;
    private double _textPositionX = 0.5;
    private double _textPositionY = 0.78;
    private double _progress;
    private string _status = string.Empty;
    private bool _isExporting;

    public MusicVideoViewModel(IMusicVideoService service) => _service = service;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string AudioPath { get => _audioPath; set => Set(ref _audioPath, value); }
    public string ImagePath { get => _imagePath; set => Set(ref _imagePath, value); }
    public string OutputPath { get => _outputPath; set => Set(ref _outputPath, value); }
    public string Title { get => _title; set => Set(ref _title, value); }
    public string Subtitle { get => _subtitle; set => Set(ref _subtitle, value); }
    public int Width { get => _width; set => Set(ref _width, value); }
    public int Height { get => _height; set => Set(ref _height, value); }
    public MusicVideoImageMode ImageMode { get => _imageMode; set => Set(ref _imageMode, value); }
    public MusicVideoAnimation Animation { get => _animation; set => Set(ref _animation, value); }
    public MusicVideoAnimationDirection AnimationDirection { get => _animationDirection; set => Set(ref _animationDirection, value); }
    public double AnimationStrength { get => _animationStrength; set => Set(ref _animationStrength, value); }
    public double BackgroundBlur { get => _backgroundBlur; set => Set(ref _backgroundBlur, Math.Clamp(value, 0, 60)); }
    public double BackgroundDim { get => _backgroundDim; set => Set(ref _backgroundDim, Math.Clamp(value, 0, 0.7)); }
    public double ImageScale { get => _imageScale; set => Set(ref _imageScale, value); }
    public double ImagePositionX { get => _imagePositionX; set => Set(ref _imagePositionX, Math.Clamp(value, -1, 1)); }
    public double ImagePositionY { get => _imagePositionY; set => Set(ref _imagePositionY, Math.Clamp(value, -1, 1)); }
    public double TextPositionX { get => _textPositionX; set => Set(ref _textPositionX, Math.Clamp(value, 0, 1)); }
    public double TextPositionY { get => _textPositionY; set => Set(ref _textPositionY, Math.Clamp(value, 0, 1)); }
    public double Progress { get => _progress; private set => Set(ref _progress, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public bool IsExporting { get => _isExporting; private set => Set(ref _isExporting, value); }

    public MusicVideoOptions CreateOptions() => new()
    {
        AudioPath = AudioPath.Trim(),
        ImagePath = ImagePath.Trim(),
        OutputPath = OutputPath.Trim(),
        Title = Title.Trim(),
        Subtitle = Subtitle.Trim(),
        Width = Width,
        Height = Height,
        ImageMode = ImageMode,
        Animation = Animation,
        AnimationDirection = AnimationDirection,
        AnimationStrength = AnimationStrength,
        BackgroundBlur = BackgroundBlur,
        BackgroundDim = BackgroundDim,
        ImageScale = ImageScale,
        ImagePositionX = ImagePositionX,
        ImagePositionY = ImagePositionY,
        TextPositionX = TextPositionX,
        TextPositionY = TextPositionY
    };

    public string? Validate()
    {
        if (!File.Exists(AudioPath))
            return "Bitte eine vorhandene Audiodatei auswählen.";
        if (!File.Exists(ImagePath))
            return "Bitte eine vorhandene Bilddatei auswählen.";
        if (string.IsNullOrWhiteSpace(OutputPath))
            return "Bitte einen Zielpfad auswählen.";
        if (Width < 320 || Height < 240 || Width % 2 != 0 || Height % 2 != 0)
            return "Die Auflösung muss aus geraden Zahlen bestehen und mindestens 320 × 240 sein.";
        return null;
    }

    public async Task ExportAsync()
    {
        var validationError = Validate();
        if (validationError is not null)
            throw new InvalidOperationException(validationError);

        _exportCancellation?.Dispose();
        _exportCancellation = new CancellationTokenSource();
        IsExporting = true;
        Progress = 0;
        Status = "Export wird vorbereitet …";
        var progress = new Progress<MusicVideoProgress>(update =>
        {
            Progress = update.Fraction * 100;
            Status = update.Stage;
        });

        try
        {
            await _service.CreateAsync(CreateOptions(), progress, _exportCancellation.Token);
        }
        finally
        {
            IsExporting = false;
        }
    }

    public void CancelExport() => _exportCancellation?.Cancel();

    public void ResetStatus()
    {
        Progress = 0;
        Status = string.Empty;
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
