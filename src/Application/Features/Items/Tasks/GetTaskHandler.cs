using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Tasks;

namespace Misa.Application.Features.Items.Tasks;

public record GetTaskQuery(Guid ItemId);

// Handler
public sealed class GetTaskHandler(IItemRepository repository)
{
    // Handle
    public async Task<TaskExtensionDto?> HandleAsync(GetTaskQuery query, CancellationToken ct)
    {
        var task = await repository.TryGetTaskAsync(query.ItemId ,ct);

        var formattedTasks = task?.ToTaskExtensionDto();
        
        return formattedTasks;
    }
}