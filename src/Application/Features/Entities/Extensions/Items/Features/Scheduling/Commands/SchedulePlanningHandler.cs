using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
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
            if (!AllowedFrequencies.Contains(schedule.ScheduleFrequencyType)) continue;

            if (!timeZoneProvider.IsValid(schedule.Timezone))
                throw new InvalidCastException("Timezone is not valid.");
            
            var utcNow = timeProvider.UtcNow;
            var localNow = timeZoneConverter.UtcToLocal(utcNow, schedule.Timezone);
            
            var currentLookaheadCount = await repository.GetExecutionCountPlannedAheadAsync(schedule.Id, utcNow, stoppingToken);
            
            var maxLocalLookaheadTime = schedule.ScheduleFrequencyType switch
            {
                ScheduleFrequencyType.Minutes => localNow.AddDays(3),
                ScheduleFrequencyType.Hours => localNow.AddDays(180),
                ScheduleFrequencyType.Days => localNow.AddYears(1),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            while (schedule.OccurrenceCountLimit != 0 
                   && currentLookaheadCount < schedule.LookaheadLimit
                   && timeZoneConverter.UtcToLocal(schedule.SchedulingAnchorUtc, schedule.Timezone) < maxLocalLookaheadTime)
            {
                var delta = schedule.ScheduleFrequencyType switch
                {
                    ScheduleFrequencyType.Minutes => TimeSpan.FromMinutes(schedule.FrequencyInterval),
                    ScheduleFrequencyType.Hours => TimeSpan.FromHours(schedule.FrequencyInterval),
                    ScheduleFrequencyType.Days => TimeSpan.FromDays(schedule.FrequencyInterval),
                    _ => throw new ArgumentOutOfRangeException()
                };

                do
                {
                    schedule.NextDueAtUtc = schedule.NextDueAtUtc is null 
                        ? schedule.ActiveFromUtc
                        : schedule.SchedulingAnchorUtc.Add(delta);

                    var localScheduledTimestamp = timeZoneConverter.UtcToLocal(schedule.SchedulingAnchorUtc, schedule.Timezone);
                    var localScheduledTime = TimeOnly.FromTimeSpan(localScheduledTimestamp.TimeOfDay);
                    
                    if ((schedule.StartTime is not null && localScheduledTime < schedule.StartTime) 
                        || (schedule.EndTime is not null && localScheduledTime > schedule.EndTime))
                    {
                        continue;
                    }

                    if (schedule.ByDay is not null &&
                        !schedule.ByDay.Contains((int)localScheduledTimestamp.DayOfWeek))
                    {
                        continue;   
                    }

                    if (schedule.ByMonthDay is not null && !schedule.ByMonthDay.Contains(localScheduledTimestamp.Day))
                    {
                        continue;
                    }

                    if (schedule.ByMonth is not null && !schedule.ByMonth.Contains(localScheduledTimestamp.Month))
                    {
                        continue;
                    }
                    
                    break;
                } 
                while (timeZoneConverter.UtcToLocal(schedule.SchedulingAnchorUtc, schedule.Timezone) < maxLocalLookaheadTime);

                if (timeZoneConverter.UtcToLocal(schedule.SchedulingAnchorUtc, schedule.Timezone) > localNow)
                {
                    currentLookaheadCount++;
                }
                
                var log = schedule.CreateExecutionLog(idGenerator.New(), timeProvider.UtcNow);
                schedule.ReduceOccurrenceCount();
                schedule.CheckAndUpdateNextAllowedExecution(utcNow);
                
                await repository.TryAddExecutionLogAsync(log, stoppingToken);
                
                await repository.SaveChangesAsync(stoppingToken);
            }
        }
    }
}