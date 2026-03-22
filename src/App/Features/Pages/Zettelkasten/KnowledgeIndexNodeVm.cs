using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public partial class KnowledgeIndexNodeVm : ObservableObject
{
    public KnowledgeIndexNodeVm(Func<Guid, string, Task>? onRename)
    {
        _onRename = onRename;
    }
    // ── Real node data ────────────────────────────────────────────────────────

    public Guid Id { get; init; }
    public WorkflowDto Workflow { get; init; }
    public string Title { get; init; } = string.Empty;
    [ObservableProperty] private string _editTitle = string.Empty;
    public Guid? ParentId { get; init; }

    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _isRenaming;
    [ObservableProperty] private bool _isDragTarget;

    public ObservableCollection<KnowledgeIndexNodeVm> Children { get; } = [];

    // ── Pending creation state ────────────────────────────────────────────────

    public bool IsPendingCreation { get; init; }
    public WorkflowDto PendingWorkflow { get; init; }

    [ObservableProperty] private string _pendingTitle = string.Empty;

    // Callbacks set by the ViewModel that owns this node
    private readonly Func<Guid, string, Task>? _onRename;
    private Func<string, Task>? _onCommit;
    private Action? _onCancel;
    private bool _committed;

    internal void SetCallbacks( 
        Func<string, Task> onCommit, 
        Action onCancel)
    {
        _onCommit = onCommit;
        _onCancel = onCancel;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void BeginRenaming()
    {
        IsRenaming = true;
        EditTitle = Title;
    }
    [RelayCommand]
    private async Task CommitRename()
    {
        IsRenaming = false;
        
        if (string.IsNullOrWhiteSpace(EditTitle) || _onRename is null)
            return;
            
        await _onRename.Invoke(Id, EditTitle.Trim());
    }
    [RelayCommand] private async Task CancelRename() => IsRenaming = false;
    
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
