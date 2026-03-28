using Avalonia.Controls;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle;

public partial class ChronicleView : UserControl
{
    public ChronicleView()
    {
        InitializeComponent();
    }

    private void EntriesList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var entry = e.AddedItems.Count > 0 ? e.AddedItems[0] as ChronicleEntryDto : null;
        if (DataContext is ChronicleViewModel vm)
            vm.SelectionChanged(entry);

        if (sender is ListBox lb)
            lb.SelectedItem = null;
    }
}
