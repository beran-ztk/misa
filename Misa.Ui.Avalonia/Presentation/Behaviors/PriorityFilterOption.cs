using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Base;

namespace Misa.Ui.Avalonia.Presentation.Behaviors;

public sealed partial class PriorityFilterOption : ObservableObject
{
    public PriorityContract Priority { get; }
    [ObservableProperty] private bool _isSelected;

    public PriorityFilterOption(PriorityContract priority, bool isSelected = true)
    {
        Priority = priority;
        IsSelected = isSelected;
    }
}
