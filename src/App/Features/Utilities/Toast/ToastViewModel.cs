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
    private CancellationTokenSource? _cts;

    public string  Title      { get; }
    public string? Message    { get; }
    public bool    HasMessage => !string.IsNullOrWhiteSpace(Message);

    public ToastViewModel(string title, string? message, Action dismiss, int durationMs = 4000)
    {
        Title    = title;
        Message  = message;
        _dismiss = dismiss;

        _cts = new CancellationTokenSource();
        _ = AutoDismissAsync(_cts.Token, durationMs);
    }

    private async Task AutoDismissAsync(CancellationToken ct, int durationMs)
    {
        try
        {
            await Task.Delay(durationMs, ct);
            await Dispatcher.UIThread.InvokeAsync(Dismiss);
        }
        catch (OperationCanceledException)
        {
            // Manually dismissed — nothing to do.
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
