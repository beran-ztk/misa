using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Misa.Ui.Avalonia.Shell.Components;

public partial class HeaderView : UserControl
{
    public HeaderView()
    {
        InitializeComponent();
    }

    // ── Drag & double-click ───────────────────────────────────────────────────

    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            GetWindow()?.BeginMoveDrag(e);
    }

    private void OnHeaderDoubleTapped(object? sender, TappedEventArgs e)
    {
        ToggleMaximize();
    }

    // ── Window control buttons ────────────────────────────────────────────────

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        var w = GetWindow();
        if (w is not null) w.WindowState = WindowState.Minimized;
    }

    private void OnMaximizeRestoreClick(object? sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        GetWindow()?.Close();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Window? GetWindow() => TopLevel.GetTopLevel(this) as Window;

    private void ToggleMaximize()
    {
        var w = GetWindow();
        if (w is null) return;
        w.WindowState = w.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
