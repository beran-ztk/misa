using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Music.Services;

namespace Music.Views;

public partial class MainWindow : Window
{
    private const uint TrackPopupMenuReturnCommand = 0x0100;
    private const uint TrackPopupMenuRightButton = 0x0002;
    private const uint WindowMessageSystemCommand = 0x0112;
    private const uint WindowStyleThickFrame = 0x00040000;
    private const uint WindowStyleSystemMenu = 0x00080000;
    private const uint WindowStyleMinimizeBox = 0x00020000;
    private const uint WindowStyleMaximizeBox = 0x00010000;
    private const uint WindowStyleCaption = 0x00C00000;

    private readonly MusicView _musicView = new();
    private WindowBounds? _lastNormalBounds;

    public MainWindow()
    {
        if (OperatingSystem.IsWindows())
            Win32Properties.AddWindowStylesCallback(this, AddNativeChromeWindowStyles);
        InitializeComponent();
        _lastNormalBounds = WindowPlacementStore.Apply(this)?.NormalBounds;
        ContentArea.Content = _musicView;
        PositionChanged += (_, _) => CaptureNormalBounds();
        SizeChanged += (_, _) => CaptureNormalBounds();
        Activated += (_, _) => TitleBar.Opacity = 1;
        Deactivated += (_, _) => TitleBar.Opacity = 0.72;
        UpdateChromeState();
    }

    private static (uint Style, uint ExStyle) AddNativeChromeWindowStyles(uint style, uint exStyle) =>
        ((style & ~WindowStyleCaption)
         | WindowStyleThickFrame
         | WindowStyleSystemMenu
         | WindowStyleMinimizeBox
         | WindowStyleMaximizeBox,
         exStyle);

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UseDarkWindowsFrame();
        UpdateChromeState();
        _musicView.EnableSystemMediaControls();
        CaptureNormalBounds();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        WindowPlacementStore.Save(this, _lastNormalBounds);
        base.OnClosing(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty && MaximizeGlyph is not null)
            UpdateChromeState();
    }

    private void CaptureNormalBounds()
    {
        if (WindowState == WindowState.Normal)
            _lastNormalBounds = WindowBounds.FromWindow(this);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        for (var visual = e.Source as Visual;
             visual is not null && visual != TitleBar;
             visual = visual.GetVisualParent())
        {
            if (visual is Button)
                return;
        }

        var point = e.GetCurrentPoint(TitleBar);
        if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            ShowWindowsSystemMenu(useCursorPosition: true);
            e.Handled = true;
            return;
        }

        if (point.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            return;

        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            e.Handled = true;
            return;
        }

        BeginMoveDrag(e);
        e.Handled = true;
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            ShowWindowsSystemMenu(useCursorPosition: false);
            e.Handled = true;
        }
    }

    private void OnMinimizeClicked(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeRestoreClicked(object? sender, RoutedEventArgs e) =>
        ToggleMaximizeRestore();

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

    private void ToggleMaximizeRestore()
    {
        if (!CanResize)
            return;

        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateChromeState()
    {
        var isMaximized = WindowState == WindowState.Maximized;
        MaximizeGlyph.IsVisible = !isMaximized;
        RestoreGlyph.IsVisible = isMaximized;
        ToolTip.SetTip(MaximizeButton, isMaximized ? "Restore" : "Maximize");
        ResizeLayer.IsHitTestVisible = !isMaximized && CanResize;
        WindowFrame.BorderThickness = isMaximized ? new Thickness(0) : new Thickness(1);
    }

    private void BeginNativeResize(WindowEdge edge, PointerPressedEventArgs e)
    {
        if (!CanResize || WindowState != WindowState.Normal
                       || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        BeginResizeDrag(edge, e);
        e.Handled = true;
    }

    private void OnResizeNorthWestPressed(object? sender, PointerPressedEventArgs e) => BeginNativeResize(WindowEdge.NorthWest, e);
    private void OnResizeNorthPressed(object? sender, PointerPressedEventArgs e) => BeginNativeResize(WindowEdge.North, e);
    private void OnResizeNorthEastPressed(object? sender, PointerPressedEventArgs e) => BeginNativeResize(WindowEdge.NorthEast, e);
    private void OnResizeWestPressed(object? sender, PointerPressedEventArgs e) => BeginNativeResize(WindowEdge.West, e);
    private void OnResizeEastPressed(object? sender, PointerPressedEventArgs e) => BeginNativeResize(WindowEdge.East, e);
    private void OnResizeSouthWestPressed(object? sender, PointerPressedEventArgs e) => BeginNativeResize(WindowEdge.SouthWest, e);
    private void OnResizeSouthPressed(object? sender, PointerPressedEventArgs e) => BeginNativeResize(WindowEdge.South, e);
    private void OnResizeSouthEastPressed(object? sender, PointerPressedEventArgs e) => BeginNativeResize(WindowEdge.SouthEast, e);

    private void ShowWindowsSystemMenu(bool useCursorPosition)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = TryGetPlatformHandle()?.Handle;
        if (handle is null || handle == IntPtr.Zero)
            return;

        var menu = GetSystemMenu(handle.Value, false);
        if (menu == IntPtr.Zero)
            return;

        NativePoint point;
        if (useCursorPosition)
        {
            if (!GetCursorPos(out point))
                return;
        }
        else
        {
            point = new NativePoint(Position.X + 12, Position.Y + 36);
        }

        var command = TrackPopupMenu(
            menu,
            TrackPopupMenuReturnCommand | TrackPopupMenuRightButton,
            point.X,
            point.Y,
            0,
            handle.Value,
            IntPtr.Zero);
        if (command != 0)
            PostMessage(handle.Value, WindowMessageSystemCommand, new IntPtr(command), IntPtr.Zero);
    }

    private void UseDarkWindowsFrame()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = TryGetPlatformHandle()?.Handle;
        if (handle is null || handle == IntPtr.Zero)
            return;

        var enabled = 1;
        DwmSetWindowAttribute(handle.Value, 20, ref enabled, sizeof(int));

        // DWMWA_COLOR_NONE: keep the native resize frame functional without
        // allowing DWM to paint its light non-client border around our chrome.
        var borderColor = unchecked((int)0xFFFFFFFE);
        DwmSetWindowAttribute(handle.Value, 34, ref borderColor, sizeof(int));

        var backdropType = 2;
        DwmSetWindowAttribute(handle.Value, 38, ref backdropType, sizeof(int));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;

        public NativePoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hwnd, bool revert);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenu(
        IntPtr menu,
        uint flags,
        int x,
        int y,
        int reserved,
        IntPtr hwnd,
        IntPtr rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
}
