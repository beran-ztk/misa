using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Notifications;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Chronicle;
using Misa.Ui.Avalonia.Features.Pages.Schedules.Root;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using Misa.Ui.Avalonia.Features.Pages.Zettelkasten;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Shell.Components;

public interface IWorkspaceHost
{
    ViewModelBase? Workspace { get; set; }
}

public partial class WorkspaceNavigationViewModel(
    IWorkspaceHost host,
    ISelectionContextState selectionContextState,
    TaskFacadeViewModel task,
    ScheduleFacadeViewModel schedule,
    ChronicleViewModel chronicle,
    ZettelkastenViewModel zettel)
    : ViewModelBase
{
    // ── Collapse / expand ─────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavWidth))]
    [NotifyPropertyChangedFor(nameof(CollapseToggleGlyph))]
    [NotifyPropertyChangedFor(nameof(CollapseToggleTooltip))]
    private bool _isCollapsed = true;

    /// <summary>Pixel width driven by collapse state; consumed by the view's Border.Width.</summary>
    public double NavWidth             => IsCollapsed ? 48 : 150;
    public string CollapseToggleGlyph   => IsCollapsed ? "»" : "«";
    public string CollapseToggleTooltip => IsCollapsed ? "Expand navigation" : "Collapse navigation";

    [RelayCommand]
    private void ToggleCollapsed() => IsCollapsed = !IsCollapsed;

    // ── Active-item tracking ──────────────────────────────────────────────

    private ViewModelBase? _activeWorkspace;

    public bool IsTasksActive     => _activeWorkspace == task;
    public bool IsSchedulerActive => _activeWorkspace == schedule;
    public bool IsChronicleActive => _activeWorkspace == chronicle;
    public bool IsZettelActive    => _activeWorkspace == zettel;

    private void SetActiveWorkspace(ViewModelBase vm)
    {
        _activeWorkspace = vm;
        OnPropertyChanged(nameof(IsTasksActive));
        OnPropertyChanged(nameof(IsSchedulerActive));
        OnPropertyChanged(nameof(IsChronicleActive));
        OnPropertyChanged(nameof(IsZettelActive));
    }

    // ── Navigation commands ───────────────────────────────────────────────

    private void InitializeWorkspaceSwitch()
    {
        selectionContextState.Set(null);
    }

    [RelayCommand]
    private async Task ShowTasks()
    {
        InitializeWorkspaceSwitch();
        await task.InitializeWorkspaceAsync();
        host.Workspace = task;
        SetActiveWorkspace(task);
    }

    [RelayCommand]
    private async Task ShowScheduler()
    {
        InitializeWorkspaceSwitch();
        await schedule.InitializeWorkspaceAsync();
        host.Workspace = schedule;
        SetActiveWorkspace(schedule);
    }

    [RelayCommand]
    private async Task ShowChronicle()
    {
        InitializeWorkspaceSwitch();
        await chronicle.InitializeWorkspaceAsync();
        host.Workspace = chronicle;
        SetActiveWorkspace(chronicle);
    }

    [RelayCommand]
    private async Task ShowZettelkasten()
    {
        InitializeWorkspaceSwitch();
        await zettel.InitializeWorkspaceAsync();
        host.Workspace = zettel;
        SetActiveWorkspace(zettel);
    }

    public async Task TryAppendTaskAsync(Guid taskId)
    {
        await task.FetchAndAppendAsync(taskId);
    }

    public async Task NavigateToItemAsync(NotificationLinkTarget target)
    {
        switch (target.Workspace)
        {
            case NotificationWorkspaceTarget.Tasks:
                selectionContextState.Set(null);
                await task.InitializeWorkspaceAsync();
                host.Workspace = task;
                SetActiveWorkspace(task);
                var taskItem = task.State.FilteredItems.FirstOrDefault(t => t.Item.Id == target.ItemId);
                if (taskItem is not null)
                    task.State.SelectedItem = taskItem;
                break;

            case NotificationWorkspaceTarget.Schedules:
                selectionContextState.Set(null);
                await schedule.InitializeWorkspaceAsync();
                host.Workspace = schedule;
                SetActiveWorkspace(schedule);
                var scheduleItem = schedule.State.FilteredItems.FirstOrDefault(s => s.Item.Id == target.ItemId);
                if (scheduleItem is not null)
                    schedule.State.SelectedItem = scheduleItem;
                break;
        }
    }
}
