using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Items.Components.Tasks;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Schedules.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Schedules.Create;

public sealed partial class CreateScheduleViewModel(ScheduleGateway gateway)
    : ViewModelBase, IHostedForm<ScheduleDto>
{
    public string FormTitle { get; } = "Create Schedule";
    public string? FormDescription { get; }

    public async Task<Result<ScheduleDto>> SubmitAsync()
    {
        var dto = TryGetValidatedRequestObject();
        if (dto is null)
            return Result<ScheduleDto>.Failure(message: "Validation failed.");

        return await gateway.CreateAsync(dto);
    }
    
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private int _frequencyInterval = 1;
    [ObservableProperty] private int _lookaheadLimit = 1;

    [ObservableProperty] private bool _hasValidationError;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int? _occurrenceCountLimit;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreateTaskAction))]
    private ScheduleActionTypeDto _selectedActionType = ScheduleActionTypeDto.None;
    public IReadOnlyList<ScheduleActionTypeDto> ActionTypes { get; } = Enum.GetValues<ScheduleActionTypeDto>();

    public bool IsCreateTaskAction => SelectedActionType == ScheduleActionTypeDto.CreateTask;

    // ── CreateTask payload ───────────────────────────────────────────
    [ObservableProperty] private string _taskTitle       = string.Empty;
    [ObservableProperty] private string? _taskDescription;
    [ObservableProperty] private TaskCategoryDto     _taskCategory = TaskCategoryDto.Personal;
    [ObservableProperty] private ActivityPriorityDto _taskPriority = ActivityPriorityDto.None;

    public IReadOnlyList<TaskCategoryDto>     TaskCategories { get; } = Enum.GetValues<TaskCategoryDto>();
    public IReadOnlyList<ActivityPriorityDto> TaskPriorities { get; } = Enum.GetValues<ActivityPriorityDto>();

    [ObservableProperty] private ScheduleMisfirePolicyDto _selectedMisfirePolicy = ScheduleMisfirePolicyDto.Catchup;
    public IReadOnlyList<ScheduleMisfirePolicyDto> MisfirePolicies { get; } = Enum.GetValues<ScheduleMisfirePolicyDto>();

    [ObservableProperty] private ScheduleFrequencyTypeDto _selectedFrequencyType = ScheduleFrequencyTypeDto.Minutes;
    public IReadOnlyList<ScheduleFrequencyTypeDto> FrequencyTypes { get; } = Enum.GetValues<ScheduleFrequencyTypeDto>();

    [ObservableProperty] private int _occurrenceTtlDays;
    [ObservableProperty] private int _occurrenceTtlHours;
    [ObservableProperty] private int _occurrenceTtlMinutes;

    [ObservableProperty] private TimeSpan? _startTime;
    [ObservableProperty] private TimeSpan? _endTime;

    [ObservableProperty] private DateTimeOffset _activeFromDate = DateTimeOffset.Now.Date;
    [ObservableProperty] private TimeSpan _activeFromTime = DateTimeOffset.Now.TimeOfDay;

    [ObservableProperty] private DateTimeOffset? _activeUntilDate;
    [ObservableProperty] private TimeSpan? _activeUntilTime;

    // Monthdays as Input "1,15,31"
    [ObservableProperty] private string _byMonthDayCsv = string.Empty;

    // Weekdays (Mo..So) and Months (Jan..Dez)
    [ObservableProperty] private bool _byDayMon;
    [ObservableProperty] private bool _byDayTue;
    [ObservableProperty] private bool _byDayWed;
    [ObservableProperty] private bool _byDayThu;
    [ObservableProperty] private bool _byDayFri;
    [ObservableProperty] private bool _byDaySat;
    [ObservableProperty] private bool _byDaySun;

    [ObservableProperty] private bool _byMonthJan;
    [ObservableProperty] private bool _byMonthFeb;
    [ObservableProperty] private bool _byMonthMar;
    [ObservableProperty] private bool _byMonthApr;
    [ObservableProperty] private bool _byMonthMay;
    [ObservableProperty] private bool _byMonthJun;
    [ObservableProperty] private bool _byMonthJul;
    [ObservableProperty] private bool _byMonthAug;
    [ObservableProperty] private bool _byMonthSep;
    [ObservableProperty] private bool _byMonthOct;
    [ObservableProperty] private bool _byMonthNov;
    [ObservableProperty] private bool _byMonthDec;

    partial void OnActiveUntilDateChanged(DateTimeOffset? value)
    {
        if (value is not null && ActiveUntilTime is null)
            ActiveUntilTime = TimeSpan.Zero;
    }

    private TimeSpan? OccurrenceTtl =>
        OccurrenceTtlDays == 0 &&
        OccurrenceTtlHours == 0 &&
        OccurrenceTtlMinutes == 0
            ? null
            : new TimeSpan(OccurrenceTtlDays, OccurrenceTtlHours, OccurrenceTtlMinutes, 0);

    private void ShowValidationError(string message)
    {
        HasValidationError = true;
        ErrorMessage = message;
    }

    private void ClearValidationError()
    {
        HasValidationError = false;
        ErrorMessage = string.Empty;
    }

    public void Reset()
    {
        Title = string.Empty;

        SelectedFrequencyType = ScheduleFrequencyTypeDto.Minutes;
        FrequencyInterval = 1;
        LookaheadLimit = 1;

        OccurrenceCountLimit = null;
        SelectedMisfirePolicy = ScheduleMisfirePolicyDto.Catchup;

        OccurrenceTtlDays = 0;
        OccurrenceTtlHours = 0;
        OccurrenceTtlMinutes = 0;

        StartTime = null;
        EndTime = null;

        ActiveFromDate = DateTimeOffset.Now.Date;
        ActiveFromTime = DateTimeOffset.Now.TimeOfDay;

        ActiveUntilDate = null;
        ActiveUntilTime = null;

        ByDayMon = ByDayTue = ByDayWed = ByDayThu = ByDayFri = ByDaySat = ByDaySun = false;
        ByMonthJan = ByMonthFeb = ByMonthMar = ByMonthApr = ByMonthMay = ByMonthJun =
            ByMonthJul = ByMonthAug = ByMonthSep = ByMonthOct = ByMonthNov = ByMonthDec = false;

        ByMonthDayCsv = string.Empty;

        SelectedActionType = ScheduleActionTypeDto.None;

        TaskTitle       = string.Empty;
        TaskDescription = null;
        TaskCategory    = TaskCategoryDto.Personal;
        TaskPriority    = ActivityPriorityDto.None;

        ClearValidationError();
    }

    private int[]? BuildByDay()
    {
        var list = new List<int>(7);
        if (ByDayMon) list.Add(1);
        if (ByDayTue) list.Add(2);
        if (ByDayWed) list.Add(3);
        if (ByDayThu) list.Add(4);
        if (ByDayFri) list.Add(5);
        if (ByDaySat) list.Add(6);
        if (ByDaySun) list.Add(0);
        return list.Count == 0 ? null : list.ToArray();
    }

    private int[]? BuildByMonth()
    {
        var list = new List<int>(12);
        if (ByMonthJan) list.Add(1);
        if (ByMonthFeb) list.Add(2);
        if (ByMonthMar) list.Add(3);
        if (ByMonthApr) list.Add(4);
        if (ByMonthMay) list.Add(5);
        if (ByMonthJun) list.Add(6);
        if (ByMonthJul) list.Add(7);
        if (ByMonthAug) list.Add(8);
        if (ByMonthSep) list.Add(9);
        if (ByMonthOct) list.Add(10);
        if (ByMonthNov) list.Add(11);
        if (ByMonthDec) list.Add(12);
        return list.Count == 0 ? null : list.ToArray();
    }

    private int[]? ParseByMonthDay()
    {
        var s = ByMonthDayCsv.Trim();
        if (string.IsNullOrWhiteSpace(s)) return null;

        var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var values = new List<int>(parts.Length);

        foreach (var p in parts)
        {
            if (!int.TryParse((string?)p, out var v) || v < 1 || v > 31)
            {
                ShowValidationError("Monthdays must be a comma-separated list of numbers between 1 and 31.");
                return null;
            }

            values.Add(v);
        }

        return values.Distinct().OrderBy(x => x).ToArray();
    }

    private CreateScheduleRequest? TryGetValidatedRequestObject()
    {
        ClearValidationError();

        var trimmedTitle = Title.Trim();
        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            ShowValidationError("Please specify a title.");
            return null;
        }

        var byMonthDay = ParseByMonthDay();
        if (HasValidationError) return null;

        string? serializedPayload = null;
        if (SelectedActionType == ScheduleActionTypeDto.CreateTask)
        {
            var taskTitle = TaskTitle.Trim();
            if (string.IsNullOrWhiteSpace(taskTitle))
            {
                ShowValidationError("Task title is required for the Create Task action.");
                return null;
            }

            var payload = new CreateTaskSchedulePayload(
                Title:       taskTitle,
                Description: string.IsNullOrWhiteSpace(TaskDescription) ? null : TaskDescription.Trim(),
                Category:    TaskCategory,
                Priority:    TaskPriority);

            serializedPayload = JsonSerializer.Serialize(payload);
        }

        return new CreateScheduleRequest
        {
            Title        = trimmedTitle,
            Description  = Description,
            TargetItemId = null,

            ScheduleFrequencyType = SelectedFrequencyType,
            FrequencyInterval     = FrequencyInterval,

            LookaheadLimit       = LookaheadLimit,
            OccurrenceCountLimit = OccurrenceCountLimit,

            ActionType = SelectedActionType,
            Payload    = serializedPayload,

            MisfirePolicy = SelectedMisfirePolicy,
            OccurrenceTtl = OccurrenceTtl,

            StartTime = StartTime is null ? null : TimeOnly.FromTimeSpan(StartTime.Value),
            EndTime   = EndTime   is null ? null : TimeOnly.FromTimeSpan(EndTime.Value),

            ActiveFromLocal  = ActiveFromDate.Add(ActiveFromTime),
            ActiveUntilLocal = ActiveUntilDate?.Add(ActiveUntilTime ?? TimeSpan.Zero),

            ByDay      = BuildByDay(),
            ByMonth    = BuildByMonth(),
            ByMonthDay = byMonthDay
        };
    }
}
