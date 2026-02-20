using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Items.Components.Schedules;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Items.Components.Schedules;

namespace Misa.Application.Mappings;

public static class ScheduleMappings
{
    public static ScheduleFrequencyType MapToDomain(this ScheduleFrequencyTypeDto dto) =>
        dto switch
        {
            ScheduleFrequencyTypeDto.Once    => ScheduleFrequencyType.Once,
            ScheduleFrequencyTypeDto.Minutes => ScheduleFrequencyType.Minutes,
            ScheduleFrequencyTypeDto.Hours   => ScheduleFrequencyType.Hours,
            ScheduleFrequencyTypeDto.Days    => ScheduleFrequencyType.Days,
            ScheduleFrequencyTypeDto.Weeks   => ScheduleFrequencyType.Weeks,
            ScheduleFrequencyTypeDto.Months  => ScheduleFrequencyType.Months,
            ScheduleFrequencyTypeDto.Years   => ScheduleFrequencyType.Years,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, null)
        };
    public static ScheduleFrequencyTypeDto MapToDto(this ScheduleFrequencyType domain) =>
        domain switch
        {
            ScheduleFrequencyType.Once    => ScheduleFrequencyTypeDto.Once,
            ScheduleFrequencyType.Minutes => ScheduleFrequencyTypeDto.Minutes,
            ScheduleFrequencyType.Hours   => ScheduleFrequencyTypeDto.Hours,
            ScheduleFrequencyType.Days    => ScheduleFrequencyTypeDto.Days,
            ScheduleFrequencyType.Weeks   => ScheduleFrequencyTypeDto.Weeks,
            ScheduleFrequencyType.Months  => ScheduleFrequencyTypeDto.Months,
            ScheduleFrequencyType.Years   => ScheduleFrequencyTypeDto.Years,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };
    public static ScheduleMisfirePolicy MapToDomain(this ScheduleMisfirePolicyDto dto) => 
        dto switch
        {
            ScheduleMisfirePolicyDto.Catchup => ScheduleMisfirePolicy.Catchup,
            ScheduleMisfirePolicyDto.Skip    => ScheduleMisfirePolicy.Skip,
            ScheduleMisfirePolicyDto.RunOnce => ScheduleMisfirePolicy.RunOnce,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, null)
        };
    public static ScheduleMisfirePolicyDto MapToDto(this ScheduleMisfirePolicy domain) => 
        domain switch
        {
            ScheduleMisfirePolicy.Catchup => ScheduleMisfirePolicyDto.Catchup,
            ScheduleMisfirePolicy.Skip    => ScheduleMisfirePolicyDto.Skip,
            ScheduleMisfirePolicy.RunOnce => ScheduleMisfirePolicyDto.RunOnce,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };
    public static ScheduleActionType ToDomain(this ScheduleActionTypeDto dto) => dto switch
    {
        ScheduleActionTypeDto.None   => ScheduleActionType.None,
        ScheduleActionTypeDto.Recurring   => ScheduleActionType.Recurring,
        ScheduleActionTypeDto.CreateTask => ScheduleActionType.CreateTask,
        _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, "Unknown ScheduleActionTypeDto.")
    };

    public static ScheduleActionTypeDto ToDto(this ScheduleActionType domain) => domain switch
    {
        ScheduleActionType.None   => ScheduleActionTypeDto.None,
        ScheduleActionType.Recurring   => ScheduleActionTypeDto.Recurring,
        ScheduleActionType.CreateTask => ScheduleActionTypeDto.CreateTask,
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown ScheduleActionType.")
    };
    public static List<ScheduleDto> ToDto(this List<ScheduleExtension> schedulers)
        => schedulers.Select(ToDto).ToList();

    public static ScheduleDto ToDto(this ScheduleExtension scheduleExtension)
        => new()
        {
            Id = scheduleExtension.Id.Value,

            FrequencyType = scheduleExtension.ScheduleFrequencyType.MapToDto(),
            FrequencyInterval = scheduleExtension.FrequencyInterval,

            OccurrenceCountLimit = scheduleExtension.OccurrenceCountLimit,

            ByDay = scheduleExtension.ByDay,
            ByMonthDay = scheduleExtension.ByMonthDay,
            ByMonth = scheduleExtension.ByMonth,

            MisfirePolicy = scheduleExtension.MisfirePolicy.MapToDto(),

            LookaheadLimit = scheduleExtension.LookaheadLimit,
            OccurrenceTtl = scheduleExtension.OccurrenceTtl,

            Payload = scheduleExtension.Payload,
            Timezone = scheduleExtension.Timezone,

            StartTime = scheduleExtension.StartTime,
            EndTime = scheduleExtension.EndTime,

            ActiveFromUtc = scheduleExtension.ActiveFromUtc,
            ActiveUntilUtc = scheduleExtension.ActiveUntilUtc,

            LastRunAtUtc = scheduleExtension.LastRunAtUtc,
            NextDueAtUtc = scheduleExtension.NextDueAtUtc,
            NextAllowedExecutionAtUtc = scheduleExtension.NextAllowedExecutionAtUtc
        };
}