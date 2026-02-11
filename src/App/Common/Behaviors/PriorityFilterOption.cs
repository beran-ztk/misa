using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Base;

namespace Misa.Ui.Avalonia.Common.Behaviors;

public sealed partial class PriorityFilterOption : ObservableObject
{
    public PriorityDto Priority { get; }
    [ObservableProperty] private bool _isSelected;

    public PriorityFilterOption(PriorityDto priority, bool isSelected = true)
    {
        Priority = priority;
        IsSelected = isSelected;
    }
}
