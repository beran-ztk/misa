using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Common.Abstractions.Time;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
public record SchedulePlanningCommand;
public class SchedulePlanningHandler(ISchedulerPlanningRepository repository, ITimeProvider timeProvider, ITimeZoneProvider timeZoneProvider)
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
            var localNow = utcNow.UtcToLocal(schedule.Timezone);
            
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
                   && schedule.SchedulingAnchorUtc.UtcToLocal(schedule.Timezone) < maxLocalLookaheadTime)
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
                    
                    var localScheduledTime = 
                        TimeOnly.FromTimeSpan(
                            schedule.SchedulingAnchorUtc
                                .UtcToLocal(schedule.Timezone)
                                    .TimeOfDay
                        );
                    
                    if ((schedule.StartTime is not null && localScheduledTime < schedule.StartTime) 
                        || (schedule.EndTime is not null && localScheduledTime > schedule.EndTime))
                    {
                        continue;
                    }

                    break;
                } 
                while (schedule.SchedulingAnchorUtc.UtcToLocal(schedule.Timezone) < maxLocalLookaheadTime);

                if (schedule.SchedulingAnchorUtc.UtcToLocal(schedule.Timezone) > localNow)
                {
                    currentLookaheadCount++;
                }
                
                var log = schedule.CreateExecutionLog();
                schedule.ReduceOccurrenceCount();
                schedule.CheckAndUpdateNextAllowedExecution(utcNow);
                
                await repository.TryAddExecutionLogAsync(log, stoppingToken);
                
                await repository.SaveChangesAsync(stoppingToken);
            }
        }
    }
}