using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items.Components.Tasks;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Create;
using Misa.Ui.Avalonia.Features.Utilities.Toast;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskFacadeViewModel : ViewModelBase
{
    private readonly TaskGateway _gateway;
    private readonly LayerProxy _layerProxy;

    public TaskState State { get; }

    public TaskFacadeViewModel(
        TaskState state,
        TaskGateway gateway,
        LayerProxy layerProxy)
    {
        State = state;
        _gateway = gateway;
        _layerProxy = layerProxy;

        State.SelectionContextState.PropertyChanged += async (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(State.SelectionContextState.UpdatedVersion):
                {
                    var id = State.SelectionContextState.ActiveEntityId;
                    await LoadCurrentModeAsync();
                    State.SelectionContextState.Set(id);
                    break;
                }
                case nameof(State.SelectionContextState.RemovedVersion):
                    await LoadCurrentModeAsync();
                    break;
            }
        };
    }

    public async Task InitializeWorkspaceAsync()
    {
        await LoadCurrentModeAsync();
    }

    // ── Refresh ─────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        State.SelectedItem = null;
        await LoadCurrentModeAsync();
    }

    private Task LoadCurrentModeAsync() => State.WorkspaceMode switch
    {
        TaskWorkspaceMode.Active   => GetActiveAsync(),
        TaskWorkspaceMode.Archived => GetArchivedAsync(),
        TaskWorkspaceMode.Deleted  => GetDeletedAsync(),
        _                          => Task.CompletedTask
    };

    private async Task GetActiveAsync()
    {
        var values = await _gateway.GetAllAsync();
        await State.SetMainCollection(values ?? []);
    }

    private async Task GetArchivedAsync()
    {
        var values = await _gateway.GetArchivedAsync();
        await State.SetMainCollection(values ?? []);
    }

    private async Task GetDeletedAsync()
    {
        var values = await _gateway.GetDeletedAsync();
        await State.SetMainCollection(values ?? []);
    }

    // ── Workspace mode toggle ───────────────────────────────────

    [RelayCommand]
    private async Task SetActiveModeAsync()
    {
        if (State.WorkspaceMode == TaskWorkspaceMode.Active) return;
        State.SelectedItem = null;
        State.WorkspaceMode = TaskWorkspaceMode.Active;
        await GetActiveAsync();
    }

    [RelayCommand]
    private async Task SetArchivedModeAsync()
    {
        if (State.WorkspaceMode == TaskWorkspaceMode.Archived) return;
        State.SelectedItem = null;
        State.WorkspaceMode = TaskWorkspaceMode.Archived;
        await GetArchivedAsync();
    }

    [RelayCommand]
    private async Task SetDeletedModeAsync()
    {
        if (State.WorkspaceMode == TaskWorkspaceMode.Deleted) return;
        State.SelectedItem = null;
        State.WorkspaceMode = TaskWorkspaceMode.Deleted;
        await GetDeletedAsync();
    }

    // ── View mode (card / list) ──────────────────────────────────

    [RelayCommand]
    private void SetCardView() => State.ViewMode = TaskViewMode.Card;

    [RelayCommand]
    private void SetListView() => State.ViewMode = TaskViewMode.List;

    // ── Add ─────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ShowAddPanelAsync()
    {
        var formVm = new CreateTaskViewModel(_gateway);

        var created = await _layerProxy.OpenAsync<CreateTaskViewModel, TaskDto>(formVm);
        if (created is null) return;

        await State.AppendToMainCollection(created);
        _layerProxy.ShowActionToast("Task created", type: ToastType.Success);
    }

    // ── Restore ─────────────────────────────────────────────────

    [RelayCommand]
    private async Task RestoreItemAsync(Guid itemId)
    {
        var item = FindItem(itemId);
        if (item is null) return;

        var result = await _gateway.RestoreAsync(itemId);
        if (!result.IsSuccess) return;

        State.RemoveFromMainCollection(item);
        _layerProxy.ShowActionToast("Item restored", type: ToastType.Success);
    }

    // ── Hard delete ──────────────────────────────────────────────

    [RelayCommand]
    private async Task HardDeleteItemAsync(Guid itemId)
    {
        var item = FindItem(itemId);
        if (item is null) return;

        var result = await _gateway.HardDeleteAsync(itemId);
        if (!result.IsSuccess) return;

        State.RemoveFromMainCollection(item);
        _layerProxy.ShowActionToast("Permanently deleted", type: ToastType.Info);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private TaskDto? FindItem(Guid id) =>
        State.FilteredItems.FirstOrDefault(t => t.Item.Id == id);
}
