using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Tasks;

namespace Misa.Application.Features.Items.Tasks;

// Request
public record GetTasksQuery;

// Handler
public class GetTasksHandler(IItemRepository repository)
{
    // Handle
    public async Task<List<TaskDto>> HandleAsync(GetTasksQuery query, CancellationToken ct)
    {
        var tasks = await repository.GetTasksAsync(ct);

        var formattedTasks = tasks.ToTaskExtensionDto();
        
        return formattedTasks;
    }
}