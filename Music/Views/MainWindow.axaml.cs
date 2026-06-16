using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Misa.Views;

public partial class MainWindow : Window
{
    private readonly Music.Views.MusicView _musicView = new();
    private readonly Music.Views.SettingsView _settingsView = new();

    public MainWindow()
    {
        InitializeComponent();
        _settingsView.PrepareForReset = () => _musicView.StopPlayback();
        _settingsView.OnResetComplete = () => _musicView.Refresh();
        _settingsView.OnMetadataChanged = () => _musicView.RefreshFilters();
        ContentArea.Content = _musicView;
        SetActiveNav(NavMusicBtn);

        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty)
                TitleMaxBtn.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        };
    }

    private void OnMusicClicked(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = _musicView;
        SetActiveNav(NavMusicBtn);
    }

    private void OnSettingsClicked(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = _settingsView;
        SetActiveNav(NavSettingsBtn);
    }

    private void SetActiveNav(Button active)
    {
        var activeBg = new SolidColorBrush(Color.FromArgb(30, 128, 128, 128));
        NavMusicBtn.Background = NavMusicBtn == active ? activeBg : Brushes.Transparent;
        NavSettingsBtn.Background = NavSettingsBtn == active ? activeBg : Brushes.Transparent;
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
