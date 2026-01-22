using Misa.Application.Common.Abstractions.Persistence;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Base;

namespace Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Queries;

public class GetTasksHandler(IItemRepository repository)
{
    public async Task<Result<List<ListTaskDto>>> Handle(
        GetTasksQuery cmd, CancellationToken ct)
    {
        var tasks = await repository.TryGetTasksAsync(ct);
        
        var result = tasks
            .Select(t => new ListTaskDto()
            {
                Id = t.Id,
                StateName = t.State.Name,
                PriorityName = t.Priority.ToString(),
                Title = t.Title
            }).ToList();

        return Result<List<ListTaskDto>>
            .Ok(result);
    }
}