using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
public record SchedulePlanningCommand;
public class SchedulePlanningHandler(ISchedulerPlanningRepository repository)
{
    public async Task HandleAsync(SchedulePlanningCommand command, CancellationToken stoppingToken)
    {
        var schedules = await repository.GetActiveSchedulesAsync(DateTimeOffset.UtcNow, stoppingToken);

        foreach (var schedule in schedules)
        {
            var lookaheadUntil = DateTimeOffset.UtcNow;
            
            switch (schedule.ScheduleFrequencyType) 
            { 
                case ScheduleFrequencyType.Minutes:
                    lookaheadUntil += TimeSpan.FromMinutes(schedule.FrequencyInterval) * schedule.LookaheadCount;
                    break;
                case ScheduleFrequencyType.Hours:
                    lookaheadUntil += TimeSpan.FromHours(schedule.FrequencyInterval) * schedule.LookaheadCount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(ScheduleFrequencyType), 
                        schedule.ScheduleFrequencyType.ToString(), 
                        null);
            }
            
            while (schedule.SchedulingAnchorUtc < lookaheadUntil)
            {
                var log = schedule.CreateExecutionLog();
            
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

                if (schedule.OccurrenceCountLimit == 0)
                {
                    break;
                }
            }
        }
    }
}