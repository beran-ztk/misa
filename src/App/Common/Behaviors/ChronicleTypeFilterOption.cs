using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Core.Features.Items.Chronicle;

namespace Misa.Ui.Avalonia.Common.Behaviors;

public sealed partial class ChronicleTypeFilterOption : ObservableObject
{
    public ChronicleEntryType Type { get; }
    public string Label { get; }

    [ObservableProperty] private bool _isSelected;

    public IRelayCommand ToggleCommand { get; }

    public ChronicleTypeFilterOption(ChronicleEntryType type, bool isSelected)
    {
        Type = type;
        Label = type.ToString();
        IsSelected = isSelected;
        ToggleCommand = new RelayCommand(() => IsSelected = !IsSelected);
    }
}
