using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity.Sessions;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed partial class InspectorState : ObservableObject
{
    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HasItem))]
    [NotifyPropertyChangedFor(nameof(CanHaveActivity))]
    private ItemDto? _item;
    
    public bool HasItem => Item != null;
    public bool CanHaveActivity => Item?.Workflow == WorkflowDto.Task;
    
    [ObservableProperty] private CurrentSessionOverviewDto? _currentSessionOverview;
}