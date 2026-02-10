using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Features.Inspector.Common;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed partial class InspectorState : ObservableObject
{
    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private ItemDto _item;
    [ObservableProperty] private DeadlineDto _deadline;
    [ObservableProperty] private IItemExtensionVm? _extension;
}