using System.Text.Json;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Common.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Messaging;
using Wolverine;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public sealed record ScheduleExecutingCommand;
public class ScheduleExecutingHandler(ISchedulerExecutingRepository repository)
{
    public async Task HandleAsync(
        ScheduleExecutingCommand command, 
        CancellationToken stoppingToken,
        IMessageBus bus)
    {
        var pendingExecutionLogs = await repository.GetPendingExecutionLogsAsync(stoppingToken);

        foreach (var log in pendingExecutionLogs)
        {
            if (log.Scheduler.ActionType != ScheduleActionType.CreateTask) continue;
            
            log.Claim(DateTimeOffset.UtcNow);
            await repository.SaveChangesAsync(stoppingToken);
            
            log.Start(DateTimeOffset.UtcNow);
            await repository.SaveChangesAsync(stoppingToken);

            if (log.Scheduler.Payload == null) continue; // Implement error
            var dto = JsonSerializer.Deserialize<AddTaskDto>(log.Scheduler.Payload);
            
            if (dto == null) continue; // Implement error
            var addTaskCommand = dto.ToCommand();
            
            var result = await bus.InvokeAsync<Result<TaskDto>>(addTaskCommand, stoppingToken);

            if (result.Status == ResultStatus.Success)
            {
                log.Succeeded(DateTimeOffset.UtcNow);
                await repository.AddOutboxMessageAsync(
                    new Outbox(EventType.SchedulerCreatedTask,
                        JsonSerializer.Serialize("Blabla"), 
                        DateTimeOffset.UtcNow), 
                    stoppingToken);
            }
            else
            {
                log.Fail(DateTimeOffset.UtcNow);
            }

            await repository.SaveChangesAsync(stoppingToken);
        }
    }
}