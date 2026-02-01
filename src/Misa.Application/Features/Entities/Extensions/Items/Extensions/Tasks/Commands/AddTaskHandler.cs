using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Common.Mappings;
using Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Wolverine;
using ItemTask = Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task;

namespace Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Commands;
public sealed record AddTaskCommand(
    string Title,
    TaskCategoryContract CategoryContract,
    PriorityContract PriorityContract,
    DeadlineInputDto? Deadline
);
public class AddTaskHandler(IItemRepository repository, IMessageBus bus)
{
    public async Task<Result<TaskDto>> HandleAsync(AddTaskCommand command, CancellationToken ct)
    {
        var task = ItemTask.Create(
            command.Title, 
            command.CategoryContract.MapToDomain(), 
            command.PriorityContract.MapToDomain()
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