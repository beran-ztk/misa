using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Common.Mappings;

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
        ScheduleActionTypeDto.Deadline   => ScheduleActionType.Deadline,
        ScheduleActionTypeDto.Recurring   => ScheduleActionType.Recurring,
        ScheduleActionTypeDto.CreateTask => ScheduleActionType.CreateTask,
        _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, "Unknown ScheduleActionTypeDto.")
    };

    public static ScheduleActionTypeDto ToDto(this ScheduleActionType domain) => domain switch
    {
        ScheduleActionType.None   => ScheduleActionTypeDto.None,
        ScheduleActionType.Deadline   => ScheduleActionTypeDto.Deadline,
        ScheduleActionType.Recurring   => ScheduleActionTypeDto.Recurring,
        ScheduleActionType.CreateTask => ScheduleActionTypeDto.CreateTask,
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown ScheduleActionType.")
    };
    public static List<ScheduleDto> ToDto(this List<Scheduler> schedulers)
        => schedulers.Select(ToDto).ToList();

    public static ScheduleDto ToDto(this Scheduler scheduler)
        => new()
        {
            Id = scheduler.Id,

            FrequencyType = scheduler.ScheduleFrequencyType.MapToDto(),
            FrequencyInterval = scheduler.FrequencyInterval,

            OccurrenceCountLimit = scheduler.OccurrenceCountLimit,

            ByDay = scheduler.ByDay,
            ByMonthDay = scheduler.ByMonthDay,
            ByMonth = scheduler.ByMonth,

            MisfirePolicy = scheduler.MisfirePolicy.MapToDto(),

            LookaheadLimit = scheduler.LookaheadLimit,
            OccurrenceTtl = scheduler.OccurrenceTtl,

            Payload = scheduler.Payload,
            Timezone = scheduler.Timezone,

            StartTime = scheduler.StartTime,
            EndTime = scheduler.EndTime,

            ActiveFromUtc = scheduler.ActiveFromUtc,
            ActiveUntilUtc = scheduler.ActiveUntilUtc,

            LastRunAtUtc = scheduler.LastRunAtUtc,
            NextDueAtUtc = scheduler.NextDueAtUtc,
            NextAllowedExecutionAtUtc = scheduler.NextAllowedExecutionAtUtc,

            Item = scheduler.Item.ToDto()
        };
}