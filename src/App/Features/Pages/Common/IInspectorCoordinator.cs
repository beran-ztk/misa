using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Misa.Ui.Avalonia.Infrastructure.States;
using InspectorViewModel = Misa.Ui.Avalonia.Features.Inspector.Base.InspectorViewModel;

namespace Misa.Ui.Avalonia.Features.Pages.Common;

public interface IInspectorCoordinator : IDisposable
{
    Task ActivateAsync();
}
public sealed class InspectorCoordinator(ISelectionContextState selectionContextState, InspectorViewModel detailVm) : IInspectorCoordinator
{
    private CancellationTokenSource? _cts;
    private bool _isActive;

    public async Task ActivateAsync()
    {
        if (_isActive) return;
        _isActive = true;

        selectionContextState.PropertyChanged += OnSelectionChanged;

        await ApplyAsync(selectionContextState.ActiveEntityId).ConfigureAwait(false);
    }

    private async void OnSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ISelectionContextState.ActiveEntityId)) return;
        await ApplyAsync(selectionContextState.ActiveEntityId).ConfigureAwait(false);
    }

    private async Task ApplyAsync(Guid? id)
    {
        Cancel();

        if (id is null)
        {
            await detailVm.Clear().ConfigureAwait(false);
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
            selectionContextState.PropertyChanged -= OnSelectionChanged;
        
        Cancel();
    }
}