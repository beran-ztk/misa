using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Items.Components.Schedules;

namespace Misa.Application.Features.Items.Schedules.Commands;
public record SchedulePlanningCommand;
public class SchedulePlanningHandler(
    ISchedulerPlanningRepository repository, 
    ITimeProvider timeProvider, 
    ITimeZoneProvider timeZoneProvider,
    ITimeZoneConverter timeZoneConverter, 
    IIdGenerator idGenerator)
{
    private static readonly HashSet<ScheduleFrequencyType> AllowedFrequencies =
    [
        ScheduleFrequencyType.Minutes,
        ScheduleFrequencyType.Hours,
        ScheduleFrequencyType.Days
    ];
    public async Task HandleAsync(SchedulePlanningCommand command, CancellationToken stoppingToken)
    {
        var schedules = await repository.GetActiveSchedulesAsync(stoppingToken);

        foreach (var schedule in schedules)
        {
            if (schedule.ScheduleExtension is null) continue;
            if (!AllowedFrequencies.Contains(schedule.ScheduleExtension.ScheduleFrequencyType)) continue;

            if (!timeZoneProvider.IsValid(string.Empty))
                throw new InvalidCastException("Timezone is not valid.");
            
            var utcNow = timeProvider.UtcNow;
            var localNow = timeZoneConverter.UtcToLocal(utcNow, string.Empty);
            
            var currentLookaheadCount = await repository.GetExecutionCountPlannedAheadAsync(schedule.ScheduleExtension.Id.Value, utcNow, stoppingToken);
            
            var maxLocalLookaheadTime = schedule.ScheduleExtension.ScheduleFrequencyType switch
            {
                ScheduleFrequencyType.Minutes => localNow.AddDays(3),
                ScheduleFrequencyType.Hours => localNow.AddDays(180),
                ScheduleFrequencyType.Days => localNow.AddYears(1),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            while (schedule.ScheduleExtension.OccurrenceCountLimit != 0 
                   && currentLookaheadCount < schedule.ScheduleExtension.LookaheadLimit
                   && timeZoneConverter.UtcToLocal(schedule.ScheduleExtension.SchedulingAnchorUtc, string.Empty) < maxLocalLookaheadTime)
            {
                var delta = schedule.ScheduleExtension.ScheduleFrequencyType switch
                {
                    ScheduleFrequencyType.Minutes => TimeSpan.FromMinutes(schedule.ScheduleExtension.FrequencyInterval),
                    ScheduleFrequencyType.Hours => TimeSpan.FromHours(schedule.ScheduleExtension.FrequencyInterval),
                    ScheduleFrequencyType.Days => TimeSpan.FromDays(schedule.ScheduleExtension.FrequencyInterval),
                    _ => throw new ArgumentOutOfRangeException()
                };

                do
                {
                    schedule.ScheduleExtension.NextDueAtUtc = schedule.ScheduleExtension.NextDueAtUtc is null 
                        ? schedule.ScheduleExtension.ActiveFromUtc
                        : schedule.ScheduleExtension.SchedulingAnchorUtc.Add(delta);

                    var localScheduledTimestamp = timeZoneConverter.UtcToLocal(schedule.ScheduleExtension.SchedulingAnchorUtc, string.Empty);
                    var localScheduledTime = TimeOnly.FromTimeSpan(localScheduledTimestamp.TimeOfDay);
                    
                    if ((schedule.ScheduleExtension.StartTime is not null && localScheduledTime < schedule.ScheduleExtension.StartTime) 
                        || (schedule.ScheduleExtension.EndTime is not null && localScheduledTime > schedule.ScheduleExtension.EndTime))
                    {
                        continue;
                    }

                    if (schedule.ScheduleExtension.ByDay is not null &&
                        !schedule.ScheduleExtension.ByDay.Contains((int)localScheduledTimestamp.DayOfWeek))
                    {
                        continue;   
                    }

                    if (schedule.ScheduleExtension.ByMonthDay is not null && !schedule.ScheduleExtension.ByMonthDay.Contains(localScheduledTimestamp.Day))
                    {
                        continue;
                    }

                    if (schedule.ScheduleExtension.ByMonth is not null && !schedule.ScheduleExtension.ByMonth.Contains(localScheduledTimestamp.Month))
                    {
                        continue;
                    }
                    
                    break;
                } 
                while (timeZoneConverter.UtcToLocal(schedule.ScheduleExtension.SchedulingAnchorUtc, string.Empty) < maxLocalLookaheadTime);

                if (timeZoneConverter.UtcToLocal(schedule.ScheduleExtension.SchedulingAnchorUtc, string.Empty) > localNow)
                {
                    currentLookaheadCount++;
                }
                
                var log = schedule.ScheduleExtension.CreateExecutionLog(idGenerator.New(), timeProvider.UtcNow);
                schedule.ScheduleExtension.ReduceOccurrenceCount();
                schedule.ScheduleExtension.CheckAndUpdateNextAllowedExecution(utcNow);
                
                await repository.TryAddExecutionLogAsync(log, stoppingToken);
                
                await repository.SaveChangesAsync(stoppingToken);
            }
        }
    }
}