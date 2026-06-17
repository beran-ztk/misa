using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Music.Views;

public partial class MainWindow : Window
{
    private readonly MusicView _musicView = new();
    private readonly SettingsView _settingsView = new();

    public MainWindow()
    {
        InitializeComponent();
        _settingsView.PrepareForReset = () => _musicView.StopPlayback();
        _settingsView.OnResetComplete = () => _musicView.Refresh();
        _settingsView.OnMetadataChanged = () => _musicView.RefreshFilters();
        ContentArea.Content = _musicView;

        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty)
                TitleMaxBtn.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        };
    }

    private void OnSettingsClicked(object? sender, RoutedEventArgs e)
    {
        if (Equals(ContentArea.Content, _musicView))
        {
            ContentArea.Content = _settingsView;
        }
        else
        {
            ContentArea.Content = _musicView;
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnTitleBarDoubleTapped(object? sender, TappedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnMinimizeClicked(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaxRestoreClicked(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnWindowCloseClicked(object? sender, RoutedEventArgs e) => Close();
}
