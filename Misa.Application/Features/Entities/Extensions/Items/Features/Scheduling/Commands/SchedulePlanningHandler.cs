using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
public record SchedulePlanningCommand;
public class SchedulePlanningHandler(ISchedulerPlanningRepository repository)
{
    public async Task HandleAsync(SchedulePlanningCommand command, CancellationToken stoppingToken)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var furthestLookaheadTimeAllowed = utcNow.AddYears(1);
        
        var schedules = await repository.GetActiveSchedulesAsync(utcNow, stoppingToken);

        foreach (var schedule in schedules)
        {
            var currentLookaheadCount = await  repository.GetExecutionCountPlannedAheadAsync(schedule.Id, utcNow, stoppingToken);
            
            while (schedule.OccurrenceCountLimit != 0 
                   && currentLookaheadCount <= schedule.LookaheadLimit
                   && schedule.SchedulingAnchorUtc < furthestLookaheadTimeAllowed)
            {
                var log = schedule.CreateExecutionLog();
                schedule.ReduceOccurrenceCount();
                schedule.CheckAndUpdateNextAllowedExecution(utcNow);
                
                switch (schedule.ScheduleFrequencyType) 
                { 
                    case ScheduleFrequencyType.Minutes:
                        schedule.NextDueAtUtc = schedule.SchedulingAnchorUtc + TimeSpan.FromMinutes(schedule.FrequencyInterval);
                        break;
                    case ScheduleFrequencyType.Hours:
                        schedule.NextDueAtUtc = schedule.SchedulingAnchorUtc + TimeSpan.FromHours(schedule.FrequencyInterval);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(ScheduleFrequencyType), 
                            schedule.ScheduleFrequencyType.ToString(), 
                            null);
                }
            
                await repository.TryAddExecutionLogAsync(log, stoppingToken);
                
                await repository.SaveChangesAsync(stoppingToken);

                if (schedule.SchedulingAnchorUtc > utcNow)
                {
                    currentLookaheadCount++;
                }
            }
        }
    }
}