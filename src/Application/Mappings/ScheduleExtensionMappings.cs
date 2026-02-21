using Misa.Contract.Items.Components.Schedules;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schedules;

namespace Misa.Application.Mappings;

public static class ScheduleExtensionMappings
{
    public static ScheduleExtensionDto ToScheduleExtensionDto(this Item item)
    {
        if (item.ScheduleExtension is null)
            throw new ArgumentNullException(nameof(item.ScheduleExtension));
        
        return new()
        {
            Item = item.ToDto(),
            Id = item.Id.Value,

            FrequencyType = item.ScheduleExtension.ScheduleFrequencyType.ToDto(),
            FrequencyInterval = item.ScheduleExtension.FrequencyInterval,

            OccurrenceCountLimit = item.ScheduleExtension.OccurrenceCountLimit,

            ByDay = item.ScheduleExtension.ByDay,
            ByMonthDay = item.ScheduleExtension.ByMonthDay,
            ByMonth = item.ScheduleExtension.ByMonth,

            MisfirePolicy = item.ScheduleExtension.MisfirePolicy.ToDto(),

            LookaheadLimit = item.ScheduleExtension.LookaheadLimit,
            OccurrenceTtl = item.ScheduleExtension.OccurrenceTtl,

            Payload = item.ScheduleExtension.Payload,
            Timezone = item.ScheduleExtension.Timezone,

            StartTime = item.ScheduleExtension.StartTime,
            EndTime = item.ScheduleExtension.EndTime,

            ActiveFromUtc = item.ScheduleExtension.ActiveFromUtc,
            ActiveUntilUtc = item.ScheduleExtension.ActiveUntilUtc,

            LastRunAtUtc = item.ScheduleExtension.LastRunAtUtc,
            NextDueAtUtc = item.ScheduleExtension.NextDueAtUtc,
            NextAllowedExecutionAtUtc = item.ScheduleExtension.NextAllowedExecutionAtUtc
        };
    }
    public static IReadOnlyCollection<ScheduleExtensionDto> ToScheduleExtensionDto(this IEnumerable<Item> schedulers)
        => schedulers.Select(s => s.ToScheduleExtensionDto()).ToList();
    public static ScheduleFrequencyType ToDomain(this ScheduleFrequencyTypeDto dto) =>
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
    public static ScheduleFrequencyTypeDto ToDto(this ScheduleFrequencyType domain) =>
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
    public static ScheduleMisfirePolicy ToDomain(this ScheduleMisfirePolicyDto dto) => 
        dto switch
        {
            ScheduleMisfirePolicyDto.Catchup => ScheduleMisfirePolicy.Catchup,
            ScheduleMisfirePolicyDto.Skip    => ScheduleMisfirePolicy.Skip,
            ScheduleMisfirePolicyDto.RunOnce => ScheduleMisfirePolicy.RunOnce,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, null)
        };
    public static ScheduleMisfirePolicyDto ToDto(this ScheduleMisfirePolicy domain) => 
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
}