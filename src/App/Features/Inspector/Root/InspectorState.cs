using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed partial class InspectorState : ObservableObject
{
    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private ItemDto _item = ItemDto.Empty();
    [ObservableProperty] private DeadlineDto? _deadline;
    
    [ObservableProperty] private CurrentSessionOverviewDto? _currentSessionOverview;
}