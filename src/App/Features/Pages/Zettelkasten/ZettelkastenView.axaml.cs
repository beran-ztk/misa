using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Zettelkasten;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public partial class ZettelkastenView : UserControl
{
    private object? _pendingContext;

    public ZettelkastenView()
    {
        InitializeComponent();
    }

    private void Tree_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ZettelkastenViewModel vm) return;
        if (sender is not TreeView tree) return;

        if (tree.SelectedItem is KnowledgeIndexEntryDto { Workflow: WorkflowDto.Zettel } entry)
            vm.SelectedZettel = vm.Zettels.FirstOrDefault(z => z.Id == entry.Id);
        else
            vm.SelectedZettel = null;
    }

    private void Add_OnPointerPressed(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            _pendingContext = button.DataContext;
            FlyoutBase.ShowAttachedFlyout(button);
        }
    }

    private async void CreateTopicMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ZettelkastenViewModel vm) return;

        if (_pendingContext is KnowledgeIndexEntryDto entry)
        {
            var parentId = entry.Workflow == WorkflowDto.Topic ? entry.Id : entry.ParentId;
            await vm.CreateTopicAsync(parentId, entry.Title);
        }
        else
        {
            await vm.CreateTopicAsync();
        }
    }

    private async void CreateZettelMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ZettelkastenViewModel vm) return;

        if (_pendingContext is KnowledgeIndexEntryDto entry)
        {
            var topicId = entry.Workflow == WorkflowDto.Topic ? entry.Id : entry.ParentId;
            await vm.CreateZettelAsync(topicId, entry.Title);
        }
        else
        {
            await vm.CreateZettelAsync();
        }
    }
}
