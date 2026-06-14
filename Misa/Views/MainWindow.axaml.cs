using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Misa.Views;

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
    }

    private void OnMusicClicked(object? sender, RoutedEventArgs e) => ContentArea.Content = _musicView;
    private void OnSettingsClicked(object? sender, RoutedEventArgs e) => ContentArea.Content = _settingsView;
}
