using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace Music.Services;

public enum MediaShortcut { Previous, PlayPause, Next }

/// <summary>Receives hardware media keys while another Windows application has focus.</summary>
public sealed class GlobalMediaKeyListener : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int HcAction = 0;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int VkMediaNextTrack = 0xB0;
    private const int VkMediaPrevTrack = 0xB1;
    private const int VkMediaPlayPause = 0xB3;

    private readonly HookProcedure _procedure;
    private IntPtr _hook;

    public event Action<MediaShortcut>? Pressed;

    public GlobalMediaKeyListener() => _procedure = HookCallback;

    public void Start()
    {
        if (!OperatingSystem.IsWindows() || _hook != IntPtr.Zero) return;
        _hook = SetWindowsHookEx(WhKeyboardLl, _procedure, GetModuleHandle(null), 0);
    }

    private IntPtr HookCallback(int code, IntPtr message, IntPtr data)
    {
        if (code == HcAction && (message.ToInt32() == WmKeyDown || message.ToInt32() == WmSysKeyDown)
            && !IsThisApplicationForeground())
        {
            var shortcut = Marshal.ReadInt32(data) switch
            {
                VkMediaPrevTrack => MediaShortcut.Previous,
                VkMediaPlayPause => MediaShortcut.PlayPause,
                VkMediaNextTrack => MediaShortcut.Next,
                _ => (MediaShortcut?)null
            };
            if (shortcut is not null)
                Dispatcher.UIThread.Post(() => Pressed?.Invoke(shortcut.Value));
        }
        return CallNextHookEx(_hook, code, message, data);
    }

    private static bool IsThisApplicationForeground()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero) return false;
        GetWindowThreadProcessId(foreground, out var processId);
        return processId == (uint)Environment.ProcessId;
    }

    public void Dispose()
    {
        if (_hook == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }

    private delegate IntPtr HookProcedure(int code, IntPtr message, IntPtr data);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProcedure procedure, IntPtr module, uint threadId);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
