using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Items.Components.Tasks;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    [ObservableProperty] private string _editTitle = string.Empty;
    [ObservableProperty] private string? _editDescription;
    
    [ObservableProperty] private ActivityStateDto? _editActivityState;
    [ObservableProperty] private ActivityPriorityDto? _editActivityPriority;
    [ObservableProperty] private TaskCategoryDto? _editTaskCategory;
    [ObservableProperty] private ScheduleMisfirePolicyDto? _editMisfirePolicy;
    [ObservableProperty] private int _editLookaheadLimit = 1;
    [ObservableProperty] private int? _editOccurrenceCountLimit;
    [ObservableProperty] private TimeSpan? _editStartTime;
    [ObservableProperty] private TimeSpan? _editEndTime;
    [ObservableProperty] private DateTimeOffset? _editActiveUntilDate;
    [ObservableProperty] private TimeSpan? _editActiveUntilTime;
    
    public string OverviewTitle => 
        Facade.State.Item.Workflow switch
        {
            WorkflowDto.Task => "Task",
            WorkflowDto.Schedule => "Schedule",
            _ => "No specific Workflow"
        };
    
    [RelayCommand]
    private void ShowEditItemForm()
    {
        if (Facade.State.IsEditItemFormOpen)
        {
            Cancel();
            return;
        }
        Facade.State.IsEditItemFormOpen = true;
        
        var item = Facade.State.Item;

        EditTitle = item.Title;
        EditDescription = item.Description;
        EditActivityState = item.Activity?.State;
        EditActivityPriority = item.Activity?.Priority;
        EditTaskCategory = item.TaskExtension?.Category;
        EditMisfirePolicy = item.ScheduleExtension?.MisfirePolicy;
        EditLookaheadLimit = item.ScheduleExtension?.LookaheadLimit ?? 1;
        EditOccurrenceCountLimit = item.ScheduleExtension?.OccurrenceCountLimit;
        EditStartTime = item.ScheduleExtension?.StartTime?.ToTimeSpan();
        EditEndTime = item.ScheduleExtension?.EndTime?.ToTimeSpan();
        var localActiveUntil = item.ScheduleExtension?.ActiveUntilUtc?.ToLocalTime();
        EditActiveUntilDate = localActiveUntil;
        EditActiveUntilTime = localActiveUntil?.TimeOfDay;
    }

    [RelayCommand]
    private void Cancel()
    {
        Facade.State.IsEditItemFormOpen = false;
        Clear();
    }

    [RelayCommand]
    private async Task Submit()
    {
        var item = Facade.State.Item;

        if (item.Workflow == WorkflowDto.Task)
        {
            var updateRequest = new UpdateTaskRequest(EditTitle, EditDescription, EditActivityState,
                EditActivityPriority, EditTaskCategory);

            var result = await Facade.Gateway.UpdateTaskAsync(item.Id, updateRequest);
            if (result is { IsSuccess: true })
            {
                Facade.ContextState.NotifyUpdated();
                await Facade.Reload();
            }
        }
        else if (item.Workflow == WorkflowDto.Schedule)
        {
            DateTimeOffset? activeUntilUtc = null;
            if (EditActiveUntilDate is { } d)
            {
                var localDt = d.Date.Add(EditActiveUntilTime ?? TimeSpan.Zero);
                activeUntilUtc = new DateTimeOffset(localDt, DateTimeOffset.Now.Offset).ToUniversalTime();
            }

            var updateRequest = new UpdateScheduleRequest(
                EditTitle,
                EditDescription,
                EditMisfirePolicy,
                EditLookaheadLimit,
                EditOccurrenceCountLimit,
                EditStartTime is { } st ? TimeOnly.FromTimeSpan(st) : null,
                EditEndTime is { } et ? TimeOnly.FromTimeSpan(et) : null,
                activeUntilUtc);

            var result = await Facade.Gateway.UpdateScheduleAsync(item.Id, updateRequest);
            if (result is { IsSuccess: true })
            {
                Facade.ContextState.NotifyUpdated();
                await Facade.Reload();
            }
        }
    }
    private void Clear()
    {
        EditTitle = string.Empty;
        EditDescription = null;
        EditActivityState = null;
        EditActivityPriority = null;
        EditTaskCategory = null;
        EditMisfirePolicy = null;
        EditLookaheadLimit = 1;
        EditOccurrenceCountLimit = null;
        EditStartTime = null;
        EditEndTime = null;
        EditActiveUntilDate = null;
        EditActiveUntilTime = null;
    }
}