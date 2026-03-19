using System;

namespace Misa.Ui.Avalonia.Features.Pages.Journal;

public sealed class JournalDayItem
{
    public int      Day        { get; init; }
    public DateTime Date       { get; init; }
    public int      EntryCount { get; init; }

    public bool HasEntries => EntryCount > 0;
    public bool IsToday    => Date == DateTime.Today;
}
