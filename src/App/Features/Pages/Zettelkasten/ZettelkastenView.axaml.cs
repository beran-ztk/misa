using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public partial class ZettelkastenView : UserControl
{
    public ZettelkastenView()
    {
        InitializeComponent();

        KnowledgeIndexTree.AddHandler(TreeViewItem.ExpandedEvent,  ExpansionStateChanged);
        KnowledgeIndexTree.AddHandler(TreeViewItem.CollapsedEvent, ExpansionStateChanged);
    }

    // ── Expansion persistence ─────────────────────────────────────────────────

    private async void ExpansionStateChanged(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not TreeViewItem { DataContext: KnowledgeIndexNodeVm node }) return;
        if (node.IsPendingCreation) return;
        if (DataContext is not ZettelkastenViewModel vm) return;

        var isExpanded = e.RoutedEvent == TreeViewItem.ExpandedEvent;
        await vm.SetExpandedStateAsync(node.Id, isExpanded);
    }

    // ── Inline creation input handling ────────────────────────────────────────

    internal void OnCreationInputLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: KnowledgeIndexNodeVm { IsPendingCreation: true } } tb)
            tb.Focus();
    }

    internal async void OnCreationInputLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: KnowledgeIndexNodeVm { IsPendingCreation: true } node })
            await node.CommitCreationCommand.ExecuteAsync(null);
    }
}
