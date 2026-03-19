using CommunityToolkit.Mvvm.ComponentModel;

namespace Misa.Ui.Avalonia.Features.Pages.Journal;

public sealed partial class JournalCalendarCell : ObservableObject
{
    public bool            IsEmpty { get; init; }
    public JournalDayItem? DayItem { get; init; }

    [ObservableProperty]
    private bool _isSelected;
}
