using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Features.Entities.Extensions.Items.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using ItemTask = Misa.Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task;

namespace Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Commands;
public record AddTaskCommand(string Title, TaskCategoryContract CategoryContract, PriorityContract PriorityContract);

public class AddTaskHandler(IItemRepository repository)
{
    public async Task<Result> HandleAsync(AddTaskCommand command, CancellationToken ct)
    {
        var task = ItemTask.Create(
            command.Title, 
            command.CategoryContract.MapToDomain(), 
            command.PriorityContract.MapToDomain()
        );

        await repository.AddAsync(task, ct);
        await repository.SaveChangesAsync(ct);
        
        return Result.Ok();
    }
}