using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Misa.Contract.Items;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public partial class ZettelkastenView : UserControl
{
    private object? _pendingContext;
    private ZettelkastenViewModel? _vm;

    public ZettelkastenView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm is not null)
            _vm.ItemCreatedAndReady -= OnItemCreatedAndReady;

        _vm = DataContext as ZettelkastenViewModel;

        if (_vm is not null)
            _vm.ItemCreatedAndReady += OnItemCreatedAndReady;
    }

    private void OnItemCreatedAndReady(KnowledgeIndexNodeVm node)
    {
        KnowledgeTree.SelectedItem = node;
    }

    private void Tree_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ZettelkastenViewModel vm) return;
        if (sender is not TreeView tree) return;

        if (tree.SelectedItem is KnowledgeIndexNodeVm { IsPending: false } node &&
            node.Entry?.Workflow == WorkflowDto.Zettel)
        {
            vm.SelectedZettel = vm.Zettels.FirstOrDefault(z => z.Id == node.Entry.Id);
        }
        else
        {
            vm.SelectedZettel = null;
        }
    }

    private void Add_OnPointerPressed(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            _pendingContext = button.DataContext;
            FlyoutBase.ShowAttachedFlyout(button);
        }
    }

    private void CreateTopicMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ZettelkastenViewModel vm) return;
        vm.BeginInlineCreate(_pendingContext as KnowledgeIndexNodeVm, WorkflowDto.Topic);
        FocusPendingTextBox();
    }

    private void CreateZettelMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ZettelkastenViewModel vm) return;
        vm.BeginInlineCreate(_pendingContext as KnowledgeIndexNodeVm, WorkflowDto.Zettel);
        FocusPendingTextBox();
    }

    private async void PendingTitleBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ZettelkastenViewModel vm) return;

        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                await vm.ConfirmInlineCreateAsync();
                break;
            case Key.Escape:
                e.Handled = true;
                vm.CancelInlineCreate();
                break;
        }
    }

    private void FocusPendingTextBox()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var tb = KnowledgeTree
                .GetVisualDescendants()
                .OfType<TextBox>()
                .FirstOrDefault(t => t.Classes.Contains("inline-create-input"));
            tb?.Focus();
        }, DispatcherPriority.Render);
    }
}
