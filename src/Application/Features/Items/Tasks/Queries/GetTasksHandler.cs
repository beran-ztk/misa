using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Tasks;
using Misa.Contract.Shared.Results;

namespace Misa.Application.Features.Items.Tasks.Queries;

// Request
public record GetTasksQuery;

// Handler
public class GetTasksHandler(
    ITaskRepository repository, 
    ICurrentUser currentUser)
{
    // Handle
    public async Task<Result<IReadOnlyCollection<TaskExtensionDto>>> HandleAsync(GetTasksQuery query, CancellationToken ct)
    {
        var tasks = await repository.GetTasksAsync(currentUser.UserId, ct);

        var formattedTasks = tasks.ToTaskExtensionDto();
        
        return Result<IReadOnlyCollection<TaskExtensionDto>>
            .Ok(formattedTasks);
    }
}