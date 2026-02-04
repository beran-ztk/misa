using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Features.Scheduler.Main;
using Misa.Ui.Avalonia.Features.Tasks.Add;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Features.Scheduler.Add;

public partial class AddScheduleViewModel(INavigationService navigationService, SchedulerMainWindowViewModel parent) : ViewModelBase
{
    private INavigationService NavigationService { get; } = navigationService;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string? _payload;
    [ObservableProperty] private int _frequencyInterval = 1;
    [ObservableProperty] private int _lookaheadLimit  = 1;

    [ObservableProperty] private bool _hasValidationError;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int? _occurrenceCountLimit;

    
    [ObservableProperty] private ScheduleActionTypeDto _selectedActionType = ScheduleActionTypeDto.None;
    public IReadOnlyList<ScheduleActionTypeDto> ActionTypes { get; } 
        = Enum.GetValues<ScheduleActionTypeDto>();
    
    partial void OnSelectedActionTypeChanged(ScheduleActionTypeDto value)
    {
        if (value == ScheduleActionTypeDto.CreateTask)
        {
            _ = ShowAddTaskWindow();
        }
    }
    private async Task ShowAddTaskWindow()
    {
        var vm = new AddTaskViewModel();
        var window = new AddTaskView()
        {
            DataContext = vm
        };
        
        AddTaskDto? result = null;

        vm.Completed += dto =>
        {
            result = dto;
            window.Close();
        };
        
        vm.Cancelled += () => window.Close();
        
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null)
        {
            window.Show();
            return;
        }

        await window.ShowDialog(owner);

        if (result != null)
        {
            Payload = JsonSerializer.Serialize(result);
        }
    }
    [ObservableProperty] private ScheduleMisfirePolicyDto _selectedMisfirePolicy = ScheduleMisfirePolicyDto.Catchup;
    public IReadOnlyList<ScheduleMisfirePolicyDto> MisfirePolicies { get; } 
        = Enum.GetValues<ScheduleMisfirePolicyDto>();
    
    [ObservableProperty] private ScheduleFrequencyTypeDto _selectedFrequencyType = ScheduleFrequencyTypeDto.Minutes;
    public IReadOnlyList<ScheduleFrequencyTypeDto> FrequencyTypes { get; } 
        = Enum.GetValues<ScheduleFrequencyTypeDto>();

    private TimeSpan? OccurrenceTtl =>
        OccurrenceTtlDays == 0 &&
        OccurrenceTtlHours == 0 &&
        OccurrenceTtlMinutes == 0
            ? null
            : new TimeSpan(
                OccurrenceTtlDays,
                OccurrenceTtlHours,
                OccurrenceTtlMinutes,
                0);

    [ObservableProperty] private int _occurrenceTtlDays;
    [ObservableProperty] private int _occurrenceTtlHours;
    [ObservableProperty] private int _occurrenceTtlMinutes;


    [ObservableProperty] private TimeSpan? _startTime;
    [ObservableProperty] private TimeSpan? _endTime;

    [ObservableProperty] private DateTimeOffset _activeFromDate = DateTimeOffset.Now.Date;
    [ObservableProperty] private TimeSpan _activeFromTime = DateTimeOffset.Now.TimeOfDay;

    [ObservableProperty] private DateTimeOffset? _activeUntilDate;
    [ObservableProperty] private TimeSpan? _activeUntilTime;
    
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

    [RelayCommand]
    private async Task SaveSchedule()
    {
        try
        {
            ClearValidationError();
            
            if (FrequencyInterval <= 0)
            {
                ShowValidationError("Frequency interval must be greater than 0");
                return;
            }
            
            var addScheduleDto = new AddScheduleDto
            {
                Title = Title,
                TargetItemId = null,
                ScheduleFrequencyType = SelectedFrequencyType,
                FrequencyInterval = FrequencyInterval,

                LookaheadLimit = LookaheadLimit,
                OccurrenceCountLimit = OccurrenceCountLimit,
                ActionType = SelectedActionType,
                Payload = Payload,
                MisfirePolicy = SelectedMisfirePolicy,
                OccurrenceTtl = OccurrenceTtl,
                StartTime = StartTime is null ? null : TimeOnly.FromTimeSpan(StartTime.Value),
                EndTime = EndTime is null ? null : TimeOnly.FromTimeSpan(EndTime.Value),
                ActiveFromLocal = ActiveFromDate.Add(ActiveFromTime),
                ActiveUntilLocal = ActiveUntilDate?.Add(ActiveUntilTime ?? TimeSpan.Zero),

                ByDay = BuildByDay(),
                ByMonth = BuildByMonth(),
                ByMonthDay = ParseByMonthDay()
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"scheduling/{NavigationService.NavigationStore.User.Id}");
            request.Content = JsonContent.Create(addScheduleDto);

            using var response = await NavigationService.NavigationStore.HttpClient
                .SendAsync(request, CancellationToken.None);
            
            var result = await response.Content.ReadFromJsonAsync<Result<ScheduleDto>>(CancellationToken.None);
            
            if (result?.IsSuccess == true && result.Value != null)
            {
                await parent.AddToCollection(result.Value);
                Close();
            }
            else
            {
                ShowValidationError(result?.Error?.Message ?? "Failed to create schedule");
            }
        }
        catch (Exception e)
        {
            ShowValidationError($"Error: {e.Message}");
        }
    }

    [RelayCommand]
    private void Close()
    {
        Title = string.Empty;
        SelectedFrequencyType = ScheduleFrequencyTypeDto.Minutes;
        FrequencyInterval = 1;
        
        OccurrenceCountLimit = null;
        SelectedMisfirePolicy = ScheduleMisfirePolicyDto.Catchup;
        
        OccurrenceTtlDays = 0;
        OccurrenceTtlHours = 0;
        OccurrenceTtlMinutes = 0;
        
        StartTime = null;
        EndTime = null;
        
        ActiveFromDate = DateTime.UtcNow.Date;
        ActiveFromTime = TimeSpan.Zero;

        ActiveUntilDate = null;
        ActiveUntilTime = null;
        
        ByDayMon = ByDayTue = ByDayWed = ByDayThu = ByDayFri = ByDaySat = ByDaySun = false;
        ByMonthJan = ByMonthFeb = ByMonthMar = ByMonthApr = ByMonthMay = ByMonthJun =
            ByMonthJul = ByMonthAug = ByMonthSep = ByMonthOct = ByMonthNov = ByMonthDec = false;
        ByMonthDayCsv = string.Empty;

        
        ClearValidationError();
    }
    
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
            if (!int.TryParse(p, out var v) || v < 1 || v > 31)
            {
                ShowValidationError("Monthdays must be a comma-separated list of numbers between 1 and 31.");
                return null;
            }
            values.Add(v);
        }

        return values.Distinct().OrderBy(x => x).ToArray();
    }
}