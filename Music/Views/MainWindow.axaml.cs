using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Music.Views;

public partial class MainWindow : Window
{
    private readonly MusicView _musicView = new();

    public MainWindow()
    {
        InitializeComponent();
        ContentArea.Content = _musicView;

        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty)
                TitleMaxBtn.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        };
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
