using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Tasks;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    [ObservableProperty] private string _editTitle = string.Empty;
    [ObservableProperty] private string? _editDescription;
    
    [ObservableProperty] private ActivityStateDto? _editActivityState;
    [ObservableProperty] private ActivityPriorityDto? _editActivityPriority;
    [ObservableProperty] private TaskCategoryDto? _editTaskCategory;
    
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
    }
    private void Clear()
    {
        EditTitle = string.Empty;
        EditDescription = null;
        EditActivityState = null;
        EditActivityPriority = null;
        EditTaskCategory = null;
    }
}