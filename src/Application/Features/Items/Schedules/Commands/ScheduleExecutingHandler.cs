using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Wolverine;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public sealed record ScheduleExecutingCommand;
public class ScheduleExecutingHandler(ISchedulerExecutingRepository repository, ITimeProvider  timeProvider, IIdGenerator idGenerator)
{
    public async Task HandleAsync(
        ScheduleExecutingCommand command, 
        CancellationToken stoppingToken,
        IMessageBus bus)
    {
        var pendingExecutionLogs = await repository.GetPendingExecutionLogsAsync(stoppingToken);

        // foreach (var log in pendingExecutionLogs)
        // {
        //     if (log.ScheduleExtension.ActionType != ScheduleActionType.CreateTask) continue;
        //     
        //     log.Claim(timeProvider.UtcNow);
        //     await repository.SaveChangesAsync(stoppingToken);
        //     
        //     log.Start(timeProvider.UtcNow);
        //     await repository.SaveChangesAsync(stoppingToken);
        //
        //     if (log.ScheduleExtension.Payload == null) continue; // Implement error
        //     var dto = JsonSerializer.Deserialize<CreateTaskDto>(log.ScheduleExtension.Payload);
        //     
        //     if (dto == null) continue; // Implement error
        //     var addTaskCommand = new CreateTaskCommand(
        //         dto.Title,
        //         dto.Description,
        //         dto.CategoryDto,
        //         dto.ActivityPriorityDto,
        //         dto.DueDate
        //     );
        //     
        //     var result = await bus.InvokeAsync<Result<TaskExtensionDto>>(addTaskCommand, stoppingToken);
        //
        //     if (result.Status == ResultStatus.Success)
        //     {
        //         log.Succeeded(timeProvider.UtcNow);
        //         await repository.AddOutboxMessageAsync(
        //             new Outbox(
        //                 idGenerator.New(),
        //                 EventType.SchedulerCreatedTask,
        //                 JsonSerializer.Serialize("Blabla"), 
        //                 timeProvider.UtcNow), 
        //             stoppingToken);
        //     }
        //     else
        //     {
        //         log.Fail(timeProvider.UtcNow);
        //     }
        //
        //     await repository.SaveChangesAsync(stoppingToken);
        // }
    }
}