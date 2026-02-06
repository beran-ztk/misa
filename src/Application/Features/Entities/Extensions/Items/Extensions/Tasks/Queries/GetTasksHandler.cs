using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;

namespace Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Queries;
public record GetTasksQuery;
public class GetTasksHandler(IItemRepository repository)
{
    public async Task<Result<List<TaskDto>>> HandleAsync(GetTasksQuery query, CancellationToken ct)
    {
        var tasks = await repository.GetTasksAsync(ct);

        return Result<List<TaskDto>>.Ok(tasks.ToDto());
    }
}