using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Misa.Ui.Avalonia.Features.Details.Page;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;

public interface IDetailCoordinator : IDisposable
{
    Task ActivateAsync();
}
public sealed class DetailCoordinator(IActiveEntitySelection selection, DetailPageViewModel detailVm) : IDetailCoordinator
{
    private CancellationTokenSource? _cts;
    private bool _isActive;

    public async Task ActivateAsync()
    {
        if (_isActive) return;
        _isActive = true;

        selection.PropertyChanged += OnSelectionChanged;

        await ApplyAsync(selection.ActiveEntityId).ConfigureAwait(false);
    }

    private async void OnSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IActiveEntitySelection.ActiveEntityId)) return;
        await ApplyAsync(selection.ActiveEntityId).ConfigureAwait(false);
    }

    private async Task ApplyAsync(Guid? id)
    {
        Cancel();

        if (id is null)
        {
            await detailVm.ResetAsync().ConfigureAwait(false);
            return;
        }
        
        _cts = new CancellationTokenSource();
        await detailVm.LoadAsync(id.Value, _cts.Token).ConfigureAwait(false);
    }

    private void Cancel()
    {
        if (_cts is null) return;
        try { _cts.Cancel(); } catch { /* ignore */ }
        _cts.Dispose();
        _cts = null;
    }
    public void Dispose()
    {
        if (_isActive)
            selection.PropertyChanged -= OnSelectionChanged;
        
        Cancel();
    }
}