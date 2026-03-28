using Misa.Contract.Items.Components.Tasks;
using Misa.Core.Abstractions.Persistence;
using Misa.Core.Mappings;

namespace Misa.Core.Features.Items.Tasks;

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