using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Misa.Contract.Items;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public partial class ZettelkastenView : UserControl
{
    // ── Drag state ────────────────────────────────────────────────────────────

    private KnowledgeIndexNodeVm? _dragSource;
    private Point                 _dragStartPos;
    private KnowledgeIndexNodeVm? _currentDropTarget;

    private const double DragThreshold = 6.0;

    public ZettelkastenView()
    {
        InitializeComponent();

        KnowledgeIndexTree.AddHandler(TreeViewItem.ExpandedEvent,  ExpansionStateChanged);
        KnowledgeIndexTree.AddHandler(TreeViewItem.CollapsedEvent, ExpansionStateChanged);

        // Drag initiation — use handledEventsToo so we intercept after TreeView selection.
        KnowledgeIndexTree.AddHandler(PointerPressedEvent,  OnTreePointerPressed,  handledEventsToo: true);
        KnowledgeIndexTree.AddHandler(PointerMovedEvent,    OnTreePointerMoved);
        KnowledgeIndexTree.AddHandler(PointerReleasedEvent, OnTreePointerReleased, handledEventsToo: true);

        // Drop handling
        KnowledgeIndexTree.AddHandler(DragDrop.DragOverEvent,  OnTreeDragOver);
        KnowledgeIndexTree.AddHandler(DragDrop.DragLeaveEvent, OnTreeDragLeave);
        KnowledgeIndexTree.AddHandler(DragDrop.DropEvent,      OnTreeDrop);
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

    // ── Drag initiation ───────────────────────────────────────────────────────

    private void OnTreePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;
        if (e.Source is TextBox) return; // don't steal from inline text inputs

        var node = FindNodeAtSource(e.Source);
        if (node is null || node.IsPendingCreation || node.IsRenaming) return;

        _dragSource   = node;
        _dragStartPos = e.GetPosition(KnowledgeIndexTree);
    }

    private void OnTreePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragSource is null) return;
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) { _dragSource = null; return; }

        var pos   = e.GetPosition(KnowledgeIndexTree);
        var delta = pos - _dragStartPos;
        if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold) return;

        var source = _dragSource;
        _dragSource = null; // clear before DoDragDrop so re-entry is safe

        var data = new DataObject();
        data.Set("DragNode", source);

        DragDrop.DoDragDrop(e, data, DragDropEffects.Move);

        // Cleanup any lingering highlight after drop completes
        ClearDropTarget();
    }

    private void OnTreePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragSource = null;
    }

    // ── Drop handling ─────────────────────────────────────────────────────────

    private void OnTreeDragOver(object? sender, DragEventArgs e)
    {
        var target = FindNodeAtSource(e.Source);

        if (target is null || !IsValidDropTarget(e, target))
        {
            ClearDropTarget();
            e.DragEffects = DragDropEffects.None;
            return;
        }

        if (_currentDropTarget != target)
        {
            ClearDropTarget();
            _currentDropTarget = target;
            target.IsDragTarget = true;
        }

        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnTreeDragLeave(object? sender, DragEventArgs e)
    {
        // Only clear if the pointer truly left the tree, not just moved between child elements.
        // DragLeave fires for every child boundary crossing; guard by checking if the new
        // position is still within our tree bounds.
        var pos    = e.GetPosition(KnowledgeIndexTree);
        var bounds = KnowledgeIndexTree.Bounds;
        if (pos.X >= 0 && pos.Y >= 0 && pos.X <= bounds.Width && pos.Y <= bounds.Height) return;

        ClearDropTarget();
    }

    private async void OnTreeDrop(object? sender, DragEventArgs e)
    {
        ClearDropTarget();

        var target = FindNodeAtSource(e.Source);
        if (target is null || !IsValidDropTarget(e, target)) return;
        if (DataContext is not ZettelkastenViewModel vm) return;
        if (e.Data.Get("DragNode") is not KnowledgeIndexNodeVm source) return;

        await vm.MoveItemAsync(source.Id, target.Id);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool IsValidDropTarget(DragEventArgs e, KnowledgeIndexNodeVm target)
    {
        if (e.Data.Get("DragNode") is not KnowledgeIndexNodeVm source) return false;
        if (target.IsPendingCreation) return false;
        if (target.Workflow != WorkflowDto.Topic) return false;
        if (target.Id == source.Id) return false;
        if (IsInSubtree(target.Id, source)) return false; // dropping onto own descendant
        return true;
    }

    // Returns true when candidateId appears anywhere in the subtree rooted at node.
    private static bool IsInSubtree(Guid candidateId, KnowledgeIndexNodeVm node)
    {
        foreach (var child in node.Children)
        {
            if (!child.IsPendingCreation && child.Id == candidateId) return true;
            if (IsInSubtree(candidateId, child)) return true;
        }
        return false;
    }

    private static KnowledgeIndexNodeVm? FindNodeAtSource(object? source)
    {
        var current = source as Visual;
        while (current is not null)
        {
            if (current is Control { DataContext: KnowledgeIndexNodeVm vm })
                return vm;
            current = current.GetVisualParent();
        }
        return null;
    }

    private void ClearDropTarget()
    {
        if (_currentDropTarget is null) return;
        _currentDropTarget.IsDragTarget = false;
        _currentDropTarget = null;
    }
}
