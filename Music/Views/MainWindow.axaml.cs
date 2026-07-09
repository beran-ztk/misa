using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Music.Services;

namespace Music.Views;

public partial class MainWindow : Window
{
    private readonly MusicView _musicView = new();
    private WindowBounds? _lastNormalBounds;

    public MainWindow()
    {
        InitializeComponent();
        _lastNormalBounds = WindowPlacementStore.Apply(this)?.NormalBounds;
        ContentArea.Content = _musicView;
        PositionChanged += (_, _) => CaptureNormalBounds();
        SizeChanged += (_, _) => CaptureNormalBounds();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UseDarkWindowsTitleBar();
        _musicView.EnableSystemMediaControls();
        CaptureNormalBounds();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        WindowPlacementStore.Save(this, _lastNormalBounds);
        base.OnClosing(e);
    }

    private void CaptureNormalBounds()
    {
        if (WindowState == WindowState.Normal)
            _lastNormalBounds = WindowBounds.FromWindow(this);
    }

    private void UseDarkWindowsTitleBar()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = TryGetPlatformHandle()?.Handle;
        if (handle is null || handle == IntPtr.Zero)
            return;

        var enabled = 1;
        DwmSetWindowAttribute(handle.Value, 20, ref enabled, sizeof(int));

        var borderColor = 0x4E4134;
        DwmSetWindowAttribute(handle.Value, 34, ref borderColor, sizeof(int));

        var backdropType = 2; // Mica where supported; ignored by older Windows builds.
        DwmSetWindowAttribute(handle.Value, 38, ref backdropType, sizeof(int));

        // COLORREF is 0x00BBGGRR. Native title bars do not support real alpha transparency here.
        var captionColor = 0x0E0B09;
        DwmSetWindowAttribute(handle.Value, 35, ref captionColor, sizeof(int));

        var textColor = 0xFFFFFF;
        DwmSetWindowAttribute(handle.Value, 36, ref textColor, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
