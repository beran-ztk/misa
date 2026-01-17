using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Items.Extensions.Tasks.Queries;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Common;

namespace Misa.Application.Items.Extensions.Tasks.Handlers;

public class GetTasksHandler(IItemRepository repository)
{
    public async Task<Result<List<ListTaskDto>>> Handle(
        GetTasksQuery cmd, CancellationToken ct)
    {
        var tasks = await repository.TryGetTasksAsync(ct);
        
        var result = tasks
            .Select(t => new ListTaskDto()
            {
                EntityId = t.EntityId,
                StateName = t.State.Name,
                PriorityName = t.Priority.Name,
                CategoryName = t.Category.Name,
                Title = t.Title
            }).ToList();

        return Result<List<ListTaskDto>>
            .Ok(result);
    }
}