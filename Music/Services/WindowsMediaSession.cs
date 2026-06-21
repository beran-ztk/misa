using System;
using Avalonia.Threading;
using Windows.Media;

namespace Music.Services;

/// <summary>
/// Registers the player with Windows' media transport system. Hardware media keys are normally
/// routed to the active system media session (for example Chrome), not as ordinary key presses.
/// </summary>
public sealed class WindowsMediaSession : IDisposable
{
    private SystemMediaTransportControls? _controls;

    public event Action<MediaShortcut>? Pressed;

    public bool Start()
    {
        if (!OperatingSystem.IsWindows() || _controls is not null) return _controls is not null;
        try
        {
            _controls = SystemMediaTransportControls.GetForCurrentView();
            _controls.IsEnabled = true;
            _controls.IsPlayEnabled = true;
            _controls.IsPauseEnabled = true;
            _controls.IsNextEnabled = true;
            _controls.IsPreviousEnabled = true;
            _controls.ButtonPressed += OnButtonPressed;
            return true;
        }
        catch
        {
            _controls = null;
            return false;
        }
    }

    public void UpdateState(EngineState state)
    {
        if (_controls is null) return;
        _controls.PlaybackStatus = state switch
        {
            EngineState.Playing => MediaPlaybackStatus.Playing,
            EngineState.Paused => MediaPlaybackStatus.Paused,
            _ => MediaPlaybackStatus.Stopped
        };
    }

    private void OnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        var shortcut = args.Button switch
        {
            SystemMediaTransportControlsButton.Previous => MediaShortcut.Previous,
            SystemMediaTransportControlsButton.Next => MediaShortcut.Next,
            SystemMediaTransportControlsButton.Play or SystemMediaTransportControlsButton.Pause => MediaShortcut.PlayPause,
            _ => (MediaShortcut?)null
        };
        if (shortcut is not null)
            Dispatcher.UIThread.Post(() => Pressed?.Invoke(shortcut.Value));
    }

    public void Dispose()
    {
        if (_controls is null) return;
        _controls.ButtonPressed -= OnButtonPressed;
        _controls.IsEnabled = false;
        _controls = null;
    }
}
