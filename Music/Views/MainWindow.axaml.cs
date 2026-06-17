using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace Music.Views;

public partial class MainWindow : Window
{
    private readonly MusicView _musicView = new();

    public MainWindow()
    {
        InitializeComponent();
        ContentArea.Content = _musicView;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UseDarkWindowsTitleBar();
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

        var captionColor = 0x000000;
        DwmSetWindowAttribute(handle.Value, 35, ref captionColor, sizeof(int));

        var textColor = 0xFFFFFF;
        DwmSetWindowAttribute(handle.Value, 36, ref textColor, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
