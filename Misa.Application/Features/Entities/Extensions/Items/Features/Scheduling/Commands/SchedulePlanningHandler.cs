using System.Data.Common;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
public record SchedulePlanningCommand;
public class SchedulePlanningHandler(ISchedulerPlanningRepository repository)
{
    public async Task HandleAsync(SchedulePlanningCommand command, CancellationToken stoppingToken)
    {
        var schedules = await repository.GetActiveSchedulesAsync(stoppingToken);

        foreach (var schedule in schedules)
        {
            while (schedule.SchedulingAnchorUtc < DateTimeOffset.UtcNow)
            {
                var anchor = schedule.SchedulingAnchorUtc;
                
                var log = SchedulerExecutionLog.Create(schedule.Id, anchor);
            
                switch (schedule.ScheduleFrequencyType) 
                { 
                    case ScheduleFrequencyType.Minutes:
                        schedule.NextDueAtUtc = anchor + TimeSpan.FromMinutes(schedule.FrequencyInterval);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                };
            
                await repository.TryAddExecutionLogAsync(log, stoppingToken);
                
                await repository.SaveChangesAsync(stoppingToken);
            }
        }
    }
}