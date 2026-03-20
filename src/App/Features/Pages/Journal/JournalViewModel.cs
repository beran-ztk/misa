using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items.Components.Chronicle;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Pages.Journal;

public sealed partial class JournalViewModel(JournalGateway gateway) : ViewModelBase
{
    // ── Raw data ──────────────────────────────────────────────────────────────

    private IReadOnlyList<ChronicleEntryDto> _monthEntries = [];

    // ── Observable state ──────────────────────────────────────────────────────

    public ObservableCollection<JournalCalendarCell> CalendarCells      { get; } = [];
    public ObservableCollection<ChronicleEntryDto>   SelectedDayEntries { get; } = [];

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
    private bool _isComposerOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitComposerCommand))]
    private string _composerTitle = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitComposerCommand))]
    private string _composerContent = string.Empty;

    // ── Derived ───────────────────────────────────────────────────────────────

    public string MonthLabel       => new DateTime(SelectedYear, SelectedMonth, 1).ToString("MMMM yyyy");
    public string SelectedDayLabel => SelectedDay?.Date.ToString("dddd, d MMMM") ?? string.Empty;
    public bool   HasEntries       => SelectedDayEntries.Count > 0;

    private bool CanSubmitComposer =>
        ComposerTitle.Trim().Length > 0 || ComposerContent.Trim().Length > 0;

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
    private void OpenComposer()
    {
        ComposerTitle   = string.Empty;
        ComposerContent = string.Empty;
        IsComposerOpen  = true;
    }

    [RelayCommand]
    private void CloseComposer()
    {
        IsComposerOpen  = false;
        ComposerTitle   = string.Empty;
        ComposerContent = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanSubmitComposer))]
    private async Task SubmitComposerAsync()
    {
        var contentTrimmed = ComposerContent.Trim();
        var titleTrimmed   = ComposerTitle.Trim();

        // Derive effective title: explicit title → first line of content → fallback
        var effectiveTitle = titleTrimmed.Length > 0
            ? titleTrimmed
            : contentTrimmed.Split('\n')[0].Trim() is { Length: > 0 } firstLine
                ? firstLine.Length <= 100 ? firstLine : firstLine[..100]
                : SelectedDay?.Date.ToString("d MMMM yyyy") ?? string.Empty;

        // OccurredAt = selected day midnight in local time → UTC
        var localMidnight  = DateTime.SpecifyKind(SelectedDay!.Date, DateTimeKind.Local);
        var occurredAtUtc  = new DateTimeOffset(localMidnight).ToUniversalTime();

        var request = new CreateJournalRequest(
            effectiveTitle,
            contentTrimmed.Length > 0 ? contentTrimmed : null,
            occurredAtUtc,
            null);

        var result = await gateway.CreateAsync(request);

        if (!result.IsSuccess) return;

        IsComposerOpen  = false;
        ComposerTitle   = string.Empty;
        ComposerContent = string.Empty;
        await LoadAsync();
    }

    // ── Other commands ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshWorkspaceAsync() => await LoadAsync();

    // ── Data loading ──────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        _monthEntries = await gateway.GetJournalsForMonthAsync(SelectedYear, SelectedMonth);
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
            foreach (var entry in _monthEntries.Where(e => e.At.ToLocalTime().Date == SelectedDay.Date))
                SelectedDayEntries.Add(entry);
        }

        OnPropertyChanged(nameof(HasEntries));
    }
}
