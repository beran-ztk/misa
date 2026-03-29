using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Domain.Items;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public partial class KnowledgeIndexNodeVm : ObservableObject
{
    public KnowledgeIndexNodeVm(Func<Guid, string, Task>? onRename)
    {
        _onRename = onRename;
        Children.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ChildCount));
            OnPropertyChanged(nameof(HasChildren));
        };
    }

    // ── Application data ─────────────────────────────────────────────────────────────

    public Guid          Id        { get; init; }
    public Workflow   Workflow  { get; init; }
    public string        Title     { get; init; } = string.Empty;
    public Guid?         ParentId  { get; init; }

    /// <summary>Non-null only for nodes built from the deleted-items list.</summary>
    public DateTimeOffset? DeletedAt { get; init; }

    public string DeletedAtDisplay =>
        DeletedAt?.LocalDateTime.ToString("MMM d, yyyy") ?? string.Empty;

    // ── UI state ──────────────────────────────────────────────────────────────

    [ObservableProperty] private string _editTitle    = string.Empty;
    [ObservableProperty] private bool   _isExpanded;
    [ObservableProperty] private bool   _isRenaming;
    [ObservableProperty] private bool   _isDragTarget;
    [ObservableProperty] private bool   _isVisible     = true;
    [ObservableProperty] private bool   _isSearchMatch = false;

    // ── Children ──────────────────────────────────────────────────────────────

    public ObservableCollection<KnowledgeIndexNodeVm> Children { get; } = [];

    public int  ChildCount  => Children.Count(c => !c.IsPendingCreation);
    public bool HasChildren => Workflow == Workflow.Topic && ChildCount > 0;

    // ── Pending creation (main tree only) ─────────────────────────────────────

    public bool        IsPendingCreation { get; init; }
    public Workflow PendingWorkflow   { get; init; }

    [ObservableProperty] private string _pendingTitle = string.Empty;

    // ── Rename callbacks ──────────────────────────────────────────────────────

    private readonly Func<Guid, string, Task>? _onRename;
    private Func<string, Task>? _onCommit;
    private Action?             _onCancel;
    private bool                _committed;

    internal void SetCallbacks(Func<string, Task> onCommit, Action onCancel)
    {
        _onCommit = onCommit;
        _onCancel = onCancel;
    }

    // ── Tree utilities ────────────────────────────────────────────────────────

    /// <summary>
    /// Yields this node's ID and the IDs of all its descendants.
    /// Pending-creation nodes are excluded (they have no real ID).
    /// </summary>
    public IEnumerable<Guid> GetSubtreeIds()
    {
        if (!IsPendingCreation)
            yield return Id;

        foreach (var child in Children)
            foreach (var id in child.GetSubtreeIds())
                yield return id;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void BeginRenaming()
    {
        IsRenaming = true;
        EditTitle  = Title;
    }

    [RelayCommand]
    private async Task CommitRename()
    {
        IsRenaming = false;

        if (string.IsNullOrWhiteSpace(EditTitle) || _onRename is null)
            return;

        await _onRename.Invoke(Id, EditTitle.Trim());
    }

    [RelayCommand]
    private void CancelRename() => IsRenaming = false;

    [RelayCommand]
    private async Task CommitCreation()
    {
        if (_committed) return;
        _committed = true;

        if (string.IsNullOrWhiteSpace(PendingTitle))
        {
            _onCancel?.Invoke();
            return;
        }

        await (_onCommit?.Invoke(PendingTitle.Trim()) ?? Task.CompletedTask);
    }

    [RelayCommand]
    private void CancelCreation()
    {
        if (_committed) return;
        _committed = true;
        _onCancel?.Invoke();
    }
}
