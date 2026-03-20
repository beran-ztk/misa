using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Zettelkasten;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class KnowledgeIndexNodeVm : ObservableObject
{
    public KnowledgeIndexNodeVm(KnowledgeIndexEntryDto entry)
    {
        Entry = entry;
        IsPending = false;
        PendingWorkflow = default;
    }

    public KnowledgeIndexNodeVm(WorkflowDto pendingWorkflow)
    {
        Entry = null;
        IsPending = true;
        PendingWorkflow = pendingWorkflow;
    }

    public KnowledgeIndexEntryDto? Entry { get; }
    public ObservableCollection<KnowledgeIndexNodeVm> Children { get; } = [];

    public bool IsPending { get; }
    public WorkflowDto PendingWorkflow { get; }

    // Unified display properties used by the normal-row template
    public string Title => Entry?.Title ?? string.Empty;
    public WorkflowDto Workflow => Entry?.Workflow ?? PendingWorkflow;

    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private string _pendingTitle = string.Empty;
}
