using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Domain.Items.Components.Activities;

namespace Misa.Ui.Avalonia.Common.Behaviors;

public sealed partial class StateFilterOption : ObservableObject
{
    public ActivityState ActivityState { get; }
    [ObservableProperty] private bool _isSelected;

    public StateFilterOption(ActivityState activityState, bool isSelected = true)
    {
        ActivityState = activityState;
        IsSelected = isSelected;
    }
}
