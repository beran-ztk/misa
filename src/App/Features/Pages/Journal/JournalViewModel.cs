using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Core.Features.Items.Chronicle;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Pages.Journal;
public sealed record ChronicleEntryDto(
    Guid? TargetItemId,
    DateTimeOffset At,
    string Title,
    ChronicleEntryType Type,
    ChronicleMetaState? MetaState,
    string? MetaText,
    string? Description
);
public sealed partial class JournalViewModel : ViewModelBase
{
    // ── Raw data ──────────────────────────────────────────────────────────────

    private IReadOnlyList<ChronicleEntryDto> _monthEntries = [];

    // ── Observable state ──────────────────────────────────────────────────────

    public ObservableCollection<JournalCalendarCell> CalendarCells      { get; } = [];
    public ObservableCollection<JournalEntryRow>     SelectedDayEntries { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthLabel))]
    private int _selectedYear = DateTime.Today.Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthLabel))]
    private int _selectedMonth = DateTime.Today.Month;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDayLabel))]
    private JournalDayItem? _selectedDay;

    // ── Composer state ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitComposerCommand))]
    private string _composerContent = string.Empty;

    [ObservableProperty]
    private TimeSpan? _composerTime = TimeSpan.Zero;

    // ── Derived ───────────────────────────────────────────────────────────────

    public string MonthLabel       => new DateTime(SelectedYear, SelectedMonth, 1).ToString("MMMM yyyy");
    public string SelectedDayLabel => SelectedDay?.Date.ToString("dddd, d MMMM") ?? string.Empty;
    public bool   HasEntries       => SelectedDayEntries.Count > 0;

    private bool CanSubmitComposer => ComposerContent.Trim().Length > 0;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public async Task InitializeWorkspaceAsync()
    {
        SelectedYear  = DateTime.Today.Year;
        SelectedMonth = DateTime.Today.Month;
        await LoadAsync();
    }

    // ── Month navigation ──────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PreviousMonthAsync()
    {
        var prev = new DateTime(SelectedYear, SelectedMonth, 1).AddMonths(-1);
        SelectedYear  = prev.Year;
        SelectedMonth = prev.Month;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task NextMonthAsync()
    {
        var next = new DateTime(SelectedYear, SelectedMonth, 1).AddMonths(1);
        SelectedYear  = next.Year;
        SelectedMonth = next.Month;
        await LoadAsync();
    }

    // ── Day selection ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void SelectDay(JournalDayItem? day)
    {
        if (day is null) return;
        SelectedDay = day;
    }

    // ── Composer ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ResetComposer()
    {
        ComposerContent = string.Empty;
        ComposerTime    = DefaultComposerTime();
    }

    [RelayCommand(CanExecute = nameof(CanSubmitComposer))]
    private async Task SubmitComposerAsync()
    {
        var content = ComposerContent.Trim();

        var localDateTime = DateTime.SpecifyKind(
            SelectedDay!.Date + (ComposerTime ?? TimeSpan.Zero),
            DateTimeKind.Local);
        var occurredAtUtc = new DateTimeOffset(localDateTime).ToUniversalTime();

        // var request = new CreateJournalRequest(
        //     "Journal entry",
        //     content,
        //     occurredAtUtc,
        //     null);

        // var result = await gateway.CreateAsync(request);
        // if (!result.IsSuccess) return;

        ResetComposer();
        await LoadAsync();
    }

    // ── Other commands ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshWorkspaceAsync() => await LoadAsync();

    // ── Data loading ──────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        // _monthEntries = await gateway.GetJournalsForMonthAsync(SelectedYear, SelectedMonth);
        RebuildCalendarCells();
    }

    private void RebuildCalendarCells()
    {
        var daysInMonth     = DateTime.DaysInMonth(SelectedYear, SelectedMonth);
        var today           = DateTime.Today;
        var prevSelectedDay = SelectedDay?.Day;

        var countsByDay = _monthEntries
            .GroupBy(e => e.At.ToLocalTime().Day)
            .ToDictionary(g => g.Key, g => g.Count());

        var firstDayOfMonth = new DateTime(SelectedYear, SelectedMonth, 1);
        var leadingEmpties  = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

        CalendarCells.Clear();

        for (var i = 0; i < leadingEmpties; i++)
            CalendarCells.Add(new JournalCalendarCell { IsEmpty = true });

        for (var d = 1; d <= daysInMonth; d++)
        {
            CalendarCells.Add(new JournalCalendarCell
            {
                DayItem = new JournalDayItem
                {
                    Day        = d,
                    Date       = new DateTime(SelectedYear, SelectedMonth, d),
                    EntryCount = countsByDay.GetValueOrDefault(d, 0)
                }
            });
        }

        var targetCell =
            (prevSelectedDay.HasValue
                ? CalendarCells.FirstOrDefault(c => !c.IsEmpty && c.DayItem!.Day == prevSelectedDay)
                : null)
            ?? (SelectedYear == today.Year && SelectedMonth == today.Month
                ? CalendarCells.FirstOrDefault(c => !c.IsEmpty && c.DayItem!.IsToday)
                : null)
            ?? CalendarCells.FirstOrDefault(c => !c.IsEmpty);

        SelectedDay = targetCell?.DayItem;
        SyncCellSelection();
        RebuildSelectedDayEntries();
    }

    partial void OnSelectedDayChanged(JournalDayItem? value)
    {
        ResetComposer();
        SyncCellSelection();
        RebuildSelectedDayEntries();
    }

    private void SyncCellSelection()
    {
        foreach (var cell in CalendarCells)
            cell.IsSelected = !cell.IsEmpty && cell.DayItem == SelectedDay;
    }

    private void RebuildSelectedDayEntries()
    {
        SelectedDayEntries.Clear();

        if (SelectedDay is not null)
        {
            // foreach (var entry in _monthEntries.Where(e => e.At.ToLocalTime().Date == SelectedDay.Date))
                // SelectedDayEntries.Add(new JournalEntryRow(entry, gateway, LoadAsync));
        }

        OnPropertyChanged(nameof(HasEntries));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Default composer time: current local time (truncated to minutes) when viewing
    /// today, 00:00 for any other day.
    /// </summary>
    private TimeSpan DefaultComposerTime()
    {
        if (SelectedDay?.IsToday == true)
        {
            var now = DateTime.Now.TimeOfDay;
            return new TimeSpan(now.Hours, now.Minutes, 0);
        }

        return TimeSpan.Zero;
    }
}
