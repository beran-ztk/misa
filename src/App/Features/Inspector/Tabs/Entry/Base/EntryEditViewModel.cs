using CommunityToolkit.Mvvm.ComponentModel;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    [ObservableProperty] private string _editTitle = string.Empty;
    [ObservableProperty] private string _editDescription = string.Empty;
}