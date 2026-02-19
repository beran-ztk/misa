using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Shared.Results;

namespace Misa.Application.Features.Entities.Extensions.Items.Extensions.Tasks.Queries;
public record GetTasksQuery();
public class GetTasksHandler(ITaskRepository repository, ICurrentUser currentUser)
{
    public async Task<Result<List<TaskDto>>> HandleAsync(GetTasksQuery query, CancellationToken ct)
    {
        var tasks = await repository.GetTasksAsync(currentUser.UserId, ct);

        var formattedTasks = tasks.ToDto();
        
        return Result<List<TaskDto>>
            .Ok(formattedTasks);
    }
}