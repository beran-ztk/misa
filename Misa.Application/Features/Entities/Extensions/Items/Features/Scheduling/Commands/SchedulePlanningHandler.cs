using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
public record SchedulePlanningCommand;
public class SchedulePlanningHandler(IItemRepository repository)
{
    public async Task HandleAsync(SchedulePlanningCommand command, CancellationToken stoppingToken)
    {
        var schedules = await repository.GetActiveSchedulesAsync(stoppingToken);

        foreach (var schedule in schedules)
        {
            DateTimeOffset? scheduledFor = schedule.ScheduleFrequencyType switch
            {   
                ScheduleFrequencyType.Once => null,
                
                ScheduleFrequencyType.Minutes => 
                    schedule.ActiveFromUtc + TimeSpan.FromMinutes(schedule.FrequencyInterval),
                
                _ => null
            };

            if (scheduledFor is null)
            {
                continue;
            }
            
            var plannedEntry = SchedulerExecutionLog.Create(
                schedulerId: schedule.Id,
                scheduledForUtc: scheduledFor.Value);
            
            await repository.AddAsync(plannedEntry, stoppingToken);
        }
        await repository.SaveChangesAsync(stoppingToken);
    }
}