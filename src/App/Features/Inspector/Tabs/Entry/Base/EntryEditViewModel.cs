using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    [ObservableProperty] private string _editTitle = string.Empty;
    [ObservableProperty] private string? _editDescription;
    
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
            Facade.State.IsEditItemFormOpen = false;
            Clear();
            return;
        }
        Facade.State.IsEditItemFormOpen = true;
        
        var item = Facade.State.Item;

        EditTitle = item.Title;
        EditDescription = item.Description;
    }

    private void Clear()
    {
        EditTitle = string.Empty;
        EditDescription = null;
    }
}