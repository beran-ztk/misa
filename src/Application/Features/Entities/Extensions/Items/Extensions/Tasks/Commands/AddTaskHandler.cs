using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Application.Mappings;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Shared.Results;
using Wolverine;
using ItemTask = Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task;

namespace Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Commands;
public sealed record AddTaskCommand(
    string Title,
    TaskCategoryContract CategoryContract,
    PriorityDto PriorityDto,
    DeadlineInputDto? Deadline
);
public class AddTaskHandler(IItemRepository repository, IMessageBus bus, ITimeProvider timeProvider, IIdGenerator idGenerator)
{
    public async Task<Result<TaskDto>> HandleAsync(AddTaskCommand command, CancellationToken ct)
    {
        var task = ItemTask.Create(
            idGenerator.New(), 
            command.Title, 
            command.CategoryContract.MapToDomain(), 
            command.PriorityDto.MapToDomain(),
            timeProvider.UtcNow
        );

        await repository.AddAsync(task, ct);
        await repository.SaveChangesAsync(ct);
        
        if (command.Deadline is not null)
        {
            var createOnce = new CreateOnceScheduleCommand(task.Id, command.Deadline.DueAtUtc);
            await bus.InvokeAsync<Result>(createOnce, ct);
        }
        
        return Result<TaskDto>.Ok(task.ToDto());
    }
}