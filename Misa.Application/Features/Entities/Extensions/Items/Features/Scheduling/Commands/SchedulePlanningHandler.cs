using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
public record SchedulePlanningCommand;
public class SchedulePlanningHandler(ISchedulerPlanningRepository repository)
{
    public async Task HandleAsync(SchedulePlanningCommand command, CancellationToken stoppingToken)
    {
        var now = DateTimeOffset.UtcNow;
        
        var schedules = await repository.GetActiveSchedulesAsync(now, stoppingToken);

        foreach (var schedule in schedules)
        {
            if (schedule.ScheduleFrequencyType != ScheduleFrequencyType.Minutes
                && schedule.ScheduleFrequencyType != ScheduleFrequencyType.Hours
                && schedule.ScheduleFrequencyType != ScheduleFrequencyType.Days)
            {
                break;
            }
            
            var currentLookaheadCount = await  repository.GetExecutionCountPlannedAheadAsync(schedule.Id, now, stoppingToken);
            
            var furthestLookaheadTimeAllowed = schedule.ScheduleFrequencyType switch
            {
                ScheduleFrequencyType.Minutes => now.AddDays(1),
                ScheduleFrequencyType.Hours => now.AddDays(30),
                ScheduleFrequencyType.Days => now.AddDays(180),
                _ => now
            };
            
            while (schedule.OccurrenceCountLimit != 0 
                   && currentLookaheadCount < schedule.LookaheadLimit
                   && schedule.SchedulingAnchorUtc < furthestLookaheadTimeAllowed)
            {
                var log = schedule.CreateExecutionLog();
                schedule.ReduceOccurrenceCount();
                schedule.CheckAndUpdateNextAllowedExecution(now);
                
                var delta = schedule.ScheduleFrequencyType switch
                {
                    ScheduleFrequencyType.Minutes => TimeSpan.FromMinutes(schedule.FrequencyInterval),
                    ScheduleFrequencyType.Hours => TimeSpan.FromHours(schedule.FrequencyInterval),
                    ScheduleFrequencyType.Days => TimeSpan.FromDays(schedule.FrequencyInterval),
                    
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(ScheduleFrequencyType),
                        schedule.ScheduleFrequencyType.ToString(),
                        null)
                };

                do
                {
                    schedule.NextDueAtUtc = schedule.SchedulingAnchorUtc.Add(delta);
                } 
                while 
                (
                    (schedule.StartTime.HasValue && TimeOnly.FromTimeSpan(schedule.SchedulingAnchorUtc.TimeOfDay) <
                        schedule.StartTime)
                    &&
                    (schedule.EndTime.HasValue && TimeOnly.FromTimeSpan(schedule.SchedulingAnchorUtc.TimeOfDay) >
                        schedule.EndTime)
                );

                if (schedule.SchedulingAnchorUtc > now)
                {
                    currentLookaheadCount++;
                }
                
                await repository.TryAddExecutionLogAsync(log, stoppingToken);
                
                await repository.SaveChangesAsync(stoppingToken);
            }
        }
    }
}