using Misa.Application.Common.Abstractions.Persistence;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public sealed record ScheduleExecutingCommand;
public class ScheduleExecutingHandler(ISchedulerExecutingRepository repository)
{
    public async Task HandleAsync(SchedulePlanningCommand command, CancellationToken stoppingToken)
    {
        
    }
}