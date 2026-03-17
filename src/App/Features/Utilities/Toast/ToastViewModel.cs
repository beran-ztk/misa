using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Utilities.Toast;

public sealed partial class ToastViewModel : ViewModelBase
{
    private readonly Action _dismiss;
    private readonly int    _durationMs;

    // Tracks how many milliseconds have already elapsed across paused segments.
    private double           _accumulatedMs;
    private DateTimeOffset   _timerStartedAt;
    private CancellationTokenSource? _cts;

    public string    Title      { get; }
    public string?   Message    { get; }
    public ToastType Type       { get; }
    public bool      HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool      IsInfo     => Type == ToastType.Info;
    public bool      IsSuccess  => Type == ToastType.Success;
    public bool      IsWarning  => Type == ToastType.Warning;

    public ToastViewModel(string title, string? message, Action dismiss, ToastType type = ToastType.Info, int durationMs = 4000)
    {
        Title       = title;
        Message     = message;
        Type        = type;
        _dismiss    = dismiss;
        _durationMs = durationMs;

        StartTimer(durationMs);
    }

    // ── Hover pause / resume ──────────────────────────────────────────────

    public void PauseAutoDismiss()
    {
        if (_cts is null) return; // already paused or dismissed

        _accumulatedMs += (DateTimeOffset.UtcNow - _timerStartedAt).TotalMilliseconds;
        _cts.Cancel();
        _cts = null;
    }

    public void ResumeAutoDismiss()
    {
        if (_cts is not null) return; // already running

        var remaining = _durationMs - _accumulatedMs;
        if (remaining <= 0)
        {
            Dismiss();
            return;
        }

        StartTimer(remaining);
    }

    // ── Internals ─────────────────────────────────────────────────────────

    private void StartTimer(double remainingMs)
    {
        _timerStartedAt = DateTimeOffset.UtcNow;
        _cts            = new CancellationTokenSource();
        _ = AutoDismissAsync(_cts.Token, (int)remainingMs);
    }

    private async Task AutoDismissAsync(CancellationToken ct, int delayMs)
    {
        try
        {
            await Task.Delay(delayMs, ct);
            await Dispatcher.UIThread.InvokeAsync(Dismiss);
        }
        catch (OperationCanceledException)
        {
            // Paused or manually dismissed — nothing to do.
        }
    }

    [RelayCommand]
    public void Dismiss()
    {
        _cts?.Cancel();
        _cts = null;
        _dismiss();
    }
}
