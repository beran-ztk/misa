using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Chronicle;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle.Root;

public sealed partial class ChronicleState(ShellState shellState, CreateJournalState createJournalState) : ObservableObject
{
    public ShellState ShellState { get; } = shellState;
    public CreateJournalState CreateState { get; } = createJournalState;
    public ObservableCollection<JournalEntryDto> JournalEntries { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    
    [ObservableProperty] private JournalEntryDto? _selectedJournal;
    public async Task AddToCollection(List<JournalEntryDto>? journals)
    {
        if (journals is null) return;

        JournalEntries.Clear();

        foreach (var journal in journals)
        {
            await AddToCollection(journal);
        }
    }

    public async Task AddToCollection(JournalEntryDto? journal)
    {
        if (journal is null) return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            JournalEntries.Add(journal);
        });
    }
}