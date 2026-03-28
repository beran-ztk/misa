using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Domain.Items.Components.Activities;

namespace Misa.Ui.Avalonia.Common.Behaviors;

public sealed partial class PriorityFilterOption : ObservableObject
{
    public ActivityPriority ActivityPriority { get; }
    [ObservableProperty] private bool _isSelected;

    public PriorityFilterOption(ActivityPriority activityPriority, bool isSelected = true)
    {
        ActivityPriority = activityPriority;
        IsSelected = isSelected;
    }
}
