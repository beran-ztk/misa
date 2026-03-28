using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Chronicle;
using Misa.Ui.Avalonia.Features.Pages.Journal;
using Misa.Ui.Avalonia.Features.Pages.Tasks;
using Misa.Ui.Avalonia.Features.Pages.Zettelkasten;
using Misa.Ui.Avalonia.Infrastructure;

namespace Misa.Ui.Avalonia.Shell.Components;

public interface IWorkspaceHost
{
    ViewModelBase? Workspace { get; set; }
}

public partial class WorkspaceNavigationViewModel(
    IWorkspaceHost host,
    ISelectionContextState selectionContextState,
    TaskFacadeViewModel task,
    JournalViewModel journal,
    ChronicleViewModel chronicle,
    ZettelkastenViewModel zettel)
    : ViewModelBase
{
    // ── Active-item tracking ──────────────────────────────────────────────

    private ViewModelBase? _activeWorkspace;

    public bool IsTasksActive     => _activeWorkspace == task;
    public bool IsJournalActive => _activeWorkspace == journal;
    public bool IsChronicleActive => _activeWorkspace == chronicle;
    public bool IsZettelActive    => _activeWorkspace == zettel;

    private void SetActiveWorkspace(ViewModelBase vm)
    {
        _activeWorkspace = vm;
        OnPropertyChanged(nameof(IsTasksActive));
        OnPropertyChanged(nameof(IsChronicleActive));
        OnPropertyChanged(nameof(IsZettelActive));
    }

    // ── Navigation commands ───────────────────────────────────────────────

    private void InitializeWorkspaceSwitch()
    {
        selectionContextState.Set(null);
    }

    [RelayCommand]
    private async Task ShowTasks()
    {
        InitializeWorkspaceSwitch();
        await task.InitializeWorkspaceAsync();
        host.Workspace = task;
        SetActiveWorkspace(task);
    }
    
    [RelayCommand]
    private async Task ShowJournal()
    {
        InitializeWorkspaceSwitch();
        await journal.InitializeWorkspaceAsync();
        host.Workspace = journal;
        SetActiveWorkspace(journal);
    }

    [RelayCommand]
    private async Task ShowChronicle()
    {
        InitializeWorkspaceSwitch();
        await chronicle.InitializeWorkspaceAsync();
        host.Workspace = chronicle;
        SetActiveWorkspace(chronicle);
    }

    [RelayCommand]
    private async Task ShowZettelkasten()
    {
        InitializeWorkspaceSwitch();
        await zettel.InitializeWorkspaceAsync();
        host.Workspace = zettel;
        SetActiveWorkspace(zettel);
    }
}
