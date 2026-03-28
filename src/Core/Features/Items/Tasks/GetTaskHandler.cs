using Misa.Contract.Items.Components.Tasks;
using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Mappings;

namespace Misa.Core.Features.Items.Tasks;

public record GetTaskQuery(Guid ItemId);

// Handler
public sealed class GetTaskHandler(IItemRepository repository)
{
    // Handle
    public async Task<TaskDto?> HandleAsync(GetTaskQuery query, CancellationToken ct)
    {
        var task = await repository.TryGetTaskAsync(query.ItemId ,ct);

        var formattedTasks = task?.ToTaskExtensionDto();
        
        return formattedTasks;
    }
}