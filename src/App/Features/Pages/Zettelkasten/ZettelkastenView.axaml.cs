using Avalonia.Controls;
using Avalonia.Interactivity;
using Misa.Contract.Items.Components.Zettelkasten;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public partial class ZettelkastenView : UserControl
{
    public ZettelkastenView()
    {
        InitializeComponent();
        
        KnowledgeIndexTree.AddHandler(TreeViewItem.ExpandedEvent, ExpansionStateChanged);
        KnowledgeIndexTree.AddHandler(TreeViewItem.CollapsedEvent, ExpansionStateChanged);
    }

    private async void ExpansionStateChanged(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not TreeViewItem { DataContext: KnowledgeIndexEntryDto entry })
        {
            return;
        }

        if (DataContext is not ZettelkastenViewModel vm) return;
        await vm.SetExpandedStateAsync(entry.Id, !entry.IsExpanded);
    }
}
