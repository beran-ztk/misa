using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items.Components.Activity;

namespace Misa.Ui.Avalonia.Common.Behaviors;

public sealed partial class PriorityFilterOption : ObservableObject
{
    public ActivityPriorityDto ActivityPriority { get; }
    [ObservableProperty] private bool _isSelected;

    public PriorityFilterOption(ActivityPriorityDto activityPriority, bool isSelected = true)
    {
        ActivityPriority = activityPriority;
        IsSelected = isSelected;
    }
}
