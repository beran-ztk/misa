using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items.Components.Activity;

namespace Misa.Ui.Avalonia.Common.Behaviors;

public sealed partial class StateFilterOption : ObservableObject
{
    public ActivityStateDto ActivityState { get; }
    [ObservableProperty] private bool _isSelected;

    public StateFilterOption(ActivityStateDto activityState, bool isSelected = true)
    {
        ActivityState = activityState;
        IsSelected = isSelected;
    }
}
