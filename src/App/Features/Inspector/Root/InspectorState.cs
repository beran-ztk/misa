using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity.Sessions;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed partial class InspectorState : ObservableObject
{
    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private ItemDto? _item;
    
    [ObservableProperty] private CurrentSessionOverviewDto? _currentSessionOverview;
}