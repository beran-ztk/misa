using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Misa.Ui.Avalonia.Common.Components.FilterDropdown;

public sealed partial class FilterOption : ObservableObject
{
    public string Label { get; }

    [ObservableProperty]
    private bool _isSelected;

    public FilterOption(string label)
    {
        Label = label;
    }

    [RelayCommand]
    private void Toggle() => IsSelected = !IsSelected;
}
