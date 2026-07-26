using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Music.Models;
using Music.Services;
using Music.ViewModels;

namespace Music.Views;

public partial class MusicVideoOverlay : UserControl
{
    private readonly MusicVideoViewModel _viewModel;
    private readonly ScaleTransform _previewImageScale = new();
    private readonly TranslateTransform _previewImageTranslate = new();
    private readonly BlurEffect _previewBackgroundBlur = new();
    private Bitmap? _previewBitmap;
    private Point? _dragStart;
    private double _dragStartX;
    private double _dragStartY;
    private bool _synchronizing;

    public event Action? CloseRequested;
    public event Action<string>? ToastRequested;

    public MusicVideoOverlay() : this(new MusicVideoViewModel(MusicVideoService.Current))
    {
    }

    internal MusicVideoOverlay(MusicVideoViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        PreviewBackgroundImage.Effect = _previewBackgroundBlur;
        PreviewImage.RenderTransform = new TransformGroup
        {
            Children = { _previewImageScale, _previewImageTranslate }
        };

        ImageModeBox.ItemsSource = new[]
        {
            new Choice<MusicVideoImageMode>("Einpassen", MusicVideoImageMode.Fit),
            new Choice<MusicVideoImageMode>("Zuschneiden", MusicVideoImageMode.Crop),
            new Choice<MusicVideoImageMode>("Unscharfer Hintergrund", MusicVideoImageMode.BlurredBackground)
        };
        AnimationBox.ItemsSource = new[]
        {
            new Choice<MusicVideoAnimation>("Keine", MusicVideoAnimation.None),
            new Choice<MusicVideoAnimation>("Langsam hineinzoomen", MusicVideoAnimation.ZoomIn),
            new Choice<MusicVideoAnimation>("Langsam herauszoomen", MusicVideoAnimation.ZoomOut),
            new Choice<MusicVideoAnimation>("Langsame Bewegung", MusicVideoAnimation.Pan)
        };
        DirectionBox.ItemsSource = new[]
        {
            new Choice<MusicVideoAnimationDirection>("Nach links", MusicVideoAnimationDirection.Left),
            new Choice<MusicVideoAnimationDirection>("Nach rechts", MusicVideoAnimationDirection.Right),
            new Choice<MusicVideoAnimationDirection>("Nach oben", MusicVideoAnimationDirection.Up),
            new Choice<MusicVideoAnimationDirection>("Nach unten", MusicVideoAnimationDirection.Down)
        };
        DragTargetBox.ItemsSource = new[] { "Bild verschieben", "Text verschieben" };
        ImageModeBox.SelectedIndex = 0;
        AnimationBox.SelectedIndex = 0;
        DirectionBox.SelectedIndex = 1;
        DragTargetBox.SelectedIndex = 0;

        AudioPathBox.TextChanged += (_, _) => _viewModel.AudioPath = AudioPathBox.Text ?? string.Empty;
        ImagePathBox.TextChanged += (_, _) =>
        {
            _viewModel.ImagePath = ImagePathBox.Text ?? string.Empty;
            LoadPreviewImage();
        };
        OutputPathBox.TextChanged += (_, _) => _viewModel.OutputPath = OutputPathBox.Text ?? string.Empty;
        TitleBox.TextChanged += (_, _) =>
        {
            _viewModel.Title = TitleBox.Text ?? string.Empty;
            UpdatePreview();
        };
        SubtitleBox.TextChanged += (_, _) =>
        {
            _viewModel.Subtitle = SubtitleBox.Text ?? string.Empty;
            UpdatePreview();
        };
        WidthBox.TextChanged += (_, _) => UpdateResolution();
        HeightBox.TextChanged += (_, _) => UpdateResolution();
        ImageModeBox.SelectionChanged += (_, _) =>
        {
            if (ImageModeBox.SelectedItem is Choice<MusicVideoImageMode> choice)
                _viewModel.ImageMode = choice.Value;
            BlurSettingsPanel.IsVisible = _viewModel.ImageMode == MusicVideoImageMode.BlurredBackground;
            ImageScaleLabel.Text = _viewModel.ImageMode == MusicVideoImageMode.BlurredBackground
                ? "VORDERGRUNDBILD-GRÖSSE"
                : "BILDSKALIERUNG";
            ImageScaleSlider.Minimum = _viewModel.ImageMode == MusicVideoImageMode.Crop ? 1 : 0.5;
            if (_viewModel.ImageMode == MusicVideoImageMode.Crop && ImageScaleSlider.Value < 1)
                ImageScaleSlider.Value = 1;
            UpdatePreview();
        };
        AnimationBox.SelectionChanged += (_, _) =>
        {
            if (AnimationBox.SelectedItem is Choice<MusicVideoAnimation> choice)
                _viewModel.Animation = choice.Value;
            DirectionBox.IsEnabled = _viewModel.Animation == MusicVideoAnimation.Pan;
        };
        DirectionBox.SelectionChanged += (_, _) =>
        {
            if (DirectionBox.SelectedItem is Choice<MusicVideoAnimationDirection> choice)
                _viewModel.AnimationDirection = choice.Value;
        };
        AnimationStrengthSlider.ValueChanged += (_, _) =>
        {
            _viewModel.AnimationStrength = AnimationStrengthSlider.Value;
            AnimationStrengthText.Text = $"{AnimationStrengthSlider.Value:P0}";
        };
        BackgroundBlurSlider.ValueChanged += (_, _) =>
        {
            _viewModel.BackgroundBlur = BackgroundBlurSlider.Value;
            BackgroundBlurText.Text = $"{BackgroundBlurSlider.Value:0} px";
            UpdatePreview();
        };
        BackgroundDimSlider.ValueChanged += (_, _) =>
        {
            _viewModel.BackgroundDim = BackgroundDimSlider.Value;
            BackgroundDimText.Text = $"{BackgroundDimSlider.Value:P0}";
            UpdatePreview();
        };
        ImageScaleSlider.ValueChanged += (_, _) =>
        {
            if (_synchronizing) return;
            _viewModel.ImageScale = ImageScaleSlider.Value;
            ImageScaleText.Text = $"{ImageScaleSlider.Value:0.00}×";
            UpdatePreview();
        };
        ImageXSlider.ValueChanged += (_, _) =>
        {
            if (_synchronizing) return;
            _viewModel.ImagePositionX = ImageXSlider.Value;
            UpdatePreview();
        };
        ImageYSlider.ValueChanged += (_, _) =>
        {
            if (_synchronizing) return;
            _viewModel.ImagePositionY = ImageYSlider.Value;
            UpdatePreview();
        };
        TextXSlider.ValueChanged += (_, _) =>
        {
            if (_synchronizing) return;
            _viewModel.TextPositionX = TextXSlider.Value;
            UpdatePreview();
        };
        TextYSlider.ValueChanged += (_, _) =>
        {
            if (_synchronizing) return;
            _viewModel.TextPositionY = TextYSlider.Value;
            UpdatePreview();
        };
        PreviewSurface.SizeChanged += (_, _) => UpdatePreview();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        DetachedFromVisualTree += (_, _) =>
        {
            _viewModel.CancelExport();
            DisposePreviewBitmap();
        };
        DirectionBox.IsEnabled = false;
        AnimationStrengthText.Text = $"{AnimationStrengthSlider.Value:P0}";
        BackgroundBlurText.Text = $"{BackgroundBlurSlider.Value:0} px";
        BackgroundDimText.Text = $"{BackgroundDimSlider.Value:P0}";
        ImageScaleText.Text = $"{ImageScaleSlider.Value:0.00}×";
        UpdateResolution();
        UpdatePreview();
    }

    public void Open()
    {
        _viewModel.ResetStatus();
        StatusText.Text = string.Empty;
        ExportProgress.IsVisible = false;
        IsVisible = true;
        UpdatePreview();
    }

    private async void OnChooseAudioClicked(object? sender, RoutedEventArgs e)
    {
        var file = await PickFileAsync(
            "Audiodatei auswählen",
            new FilePickerFileType("Audiodateien") { Patterns = ["*.mp3", "*.m4a", "*.wav", "*.flac", "*.aac", "*.ogg"] });
        if (file is null) return;
        AudioPathBox.Text = file.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
            TitleBox.Text = Path.GetFileNameWithoutExtension(file.Name);
    }

    private async void OnChooseImageClicked(object? sender, RoutedEventArgs e)
    {
        var file = await PickFileAsync(
            "Cover oder Hintergrund auswählen",
            new FilePickerFileType("Bilddateien") { Patterns = ["*.jpg", "*.jpeg", "*.png", "*.webp", "*.bmp"] });
        if (file is not null)
            ImagePathBox.Text = file.Path.LocalPath;
    }

    private async void OnChooseOutputClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;
        var suggestedName = string.IsNullOrWhiteSpace(TitleBox.Text)
            ? "musikvideo.mp4"
            : SanitizeFileName(TitleBox.Text!) + ".mp4";
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Musikvideo speichern",
            SuggestedFileName = suggestedName,
            DefaultExtension = "mp4",
            FileTypeChoices = [new FilePickerFileType("MP4-Video") { Patterns = ["*.mp4"] }]
        });
        if (file is not null)
            OutputPathBox.Text = file.Path.LocalPath;
    }

    private async Task<IStorageFile?> PickFileAsync(string title, FilePickerFileType fileType)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return null;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [fileType]
        });
        return files.Count == 0 ? null : files[0];
    }

    private async void OnExportClicked(object? sender, RoutedEventArgs e)
    {
        SyncResolution();
        var validationError = _viewModel.Validate();
        if (validationError is not null)
        {
            StatusText.Text = validationError;
            return;
        }

        try
        {
            await _viewModel.ExportAsync();
            ToastRequested?.Invoke($"Musikvideo erstellt: {Path.GetFileName(_viewModel.OutputPath)}");
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Export abgebrochen.";
        }
        catch (Exception exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private void OnCancelExportClicked(object? sender, RoutedEventArgs e) => _viewModel.CancelExport();

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.IsExporting)
            return;
        CloseRequested?.Invoke();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MusicVideoViewModel.IsExporting))
        {
            ExportButton.IsEnabled = !_viewModel.IsExporting;
            CloseButton.IsEnabled = !_viewModel.IsExporting;
            CancelExportButton.IsVisible = _viewModel.IsExporting;
            ExportProgress.IsVisible = _viewModel.IsExporting || _viewModel.Progress > 0;
        }
        else if (e.PropertyName == nameof(MusicVideoViewModel.Progress))
        {
            ExportProgress.Value = _viewModel.Progress;
        }
        else if (e.PropertyName == nameof(MusicVideoViewModel.Status))
        {
            StatusText.Text = _viewModel.Status;
        }
    }

    private void LoadPreviewImage()
    {
        DisposePreviewBitmap();
        if (!File.Exists(_viewModel.ImagePath))
        {
            PreviewImage.Source = null;
            PreviewBackgroundImage.Source = null;
            return;
        }

        try
        {
            _previewBitmap = new Bitmap(_viewModel.ImagePath);
            PreviewImage.Source = _previewBitmap;
            PreviewBackgroundImage.Source = _previewBitmap;
        }
        catch
        {
            PreviewImage.Source = null;
            PreviewBackgroundImage.Source = null;
        }
        UpdatePreview();
    }

    private void DisposePreviewBitmap()
    {
        PreviewImage.Source = null;
        PreviewBackgroundImage.Source = null;
        _previewBitmap?.Dispose();
        _previewBitmap = null;
    }

    private void UpdateResolution()
    {
        SyncResolution();
        var ratio = _viewModel.Width > 0 && _viewModel.Height > 0
            ? (double)_viewModel.Width / _viewModel.Height
            : 16d / 9d;
        var availableWidth = Math.Max(360, PreviewSurface.Bounds.Width);
        PreviewAspect.Height = Math.Clamp(availableWidth / ratio, 220, 520);
        PreviewResolutionText.Text = $"{_viewModel.Width} × {_viewModel.Height}";
        UpdatePreview();
    }

    private void SyncResolution()
    {
        if (int.TryParse(WidthBox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var width))
            _viewModel.Width = width;
        if (int.TryParse(HeightBox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var height))
            _viewModel.Height = height;
    }

    private void UpdatePreview()
    {
        var width = PreviewSurface.Bounds.Width;
        var height = PreviewAspect.Height;
        if (width <= 0 || height <= 0) return;

        PreviewBackgroundImage.IsVisible = _viewModel.ImageMode == MusicVideoImageMode.BlurredBackground;
        PreviewDimmer.IsVisible = PreviewBackgroundImage.IsVisible;
        var outputHeight = Math.Max(1, _viewModel.Height);
        _previewBackgroundBlur.Radius = _viewModel.BackgroundBlur * height / outputHeight;
        PreviewDimmer.Fill = new SolidColorBrush(Color.FromArgb(
            (byte)Math.Round(_viewModel.BackgroundDim * byte.MaxValue),
            0,
            0,
            0));
        PreviewImage.Stretch = _viewModel.ImageMode == MusicVideoImageMode.Crop
            ? Stretch.UniformToFill
            : Stretch.Uniform;
        _previewImageScale.ScaleX = _viewModel.ImageScale;
        _previewImageScale.ScaleY = _viewModel.ImageScale;
        _previewImageTranslate.X = _viewModel.ImagePositionX * width / 2;
        _previewImageTranslate.Y = _viewModel.ImagePositionY * height / 2;

        PreviewTitle.Text = _viewModel.Title;
        PreviewSubtitle.Text = _viewModel.Subtitle;
        PreviewSubtitle.IsVisible = !string.IsNullOrWhiteSpace(_viewModel.Subtitle);
        PreviewTextPanel.Width = Math.Min(500, width * 0.86);
        Canvas.SetLeft(PreviewTextPanel, width * _viewModel.TextPositionX - PreviewTextPanel.Width / 2);
        Canvas.SetTop(PreviewTextPanel, Math.Max(0, height * _viewModel.TextPositionY - 28));
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(PreviewSurface).Properties.IsLeftButtonPressed)
            return;
        _dragStart = e.GetPosition(PreviewSurface);
        if (DragTargetBox.SelectedIndex == 1)
        {
            _dragStartX = _viewModel.TextPositionX;
            _dragStartY = _viewModel.TextPositionY;
        }
        else
        {
            _dragStartX = _viewModel.ImagePositionX;
            _dragStartY = _viewModel.ImagePositionY;
        }
        e.Pointer.Capture(PreviewSurface);
        e.Handled = true;
    }

    private void OnPreviewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStart is not { } start || PreviewSurface.Bounds.Width <= 0 || PreviewAspect.Height <= 0)
            return;
        var point = e.GetPosition(PreviewSurface);
        var dx = point.X - start.X;
        var dy = point.Y - start.Y;
        _synchronizing = true;
        if (DragTargetBox.SelectedIndex == 1)
        {
            _viewModel.TextPositionX = _dragStartX + dx / PreviewSurface.Bounds.Width;
            _viewModel.TextPositionY = _dragStartY + dy / PreviewAspect.Height;
            TextXSlider.Value = _viewModel.TextPositionX;
            TextYSlider.Value = _viewModel.TextPositionY;
        }
        else
        {
            _viewModel.ImagePositionX = _dragStartX + dx * 2 / PreviewSurface.Bounds.Width;
            _viewModel.ImagePositionY = _dragStartY + dy * 2 / PreviewAspect.Height;
            ImageXSlider.Value = _viewModel.ImagePositionX;
            ImageYSlider.Value = _viewModel.ImagePositionY;
        }
        _synchronizing = false;
        UpdatePreview();
    }

    private void OnPreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragStart = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var character in Path.GetInvalidFileNameChars())
            name = name.Replace(character, '_');
        return string.IsNullOrWhiteSpace(name) ? "musikvideo" : name.Trim();
    }

    private sealed record Choice<T>(string Label, T Value)
    {
        public override string ToString() => Label;
    }
}
