using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Tasks;

namespace Misa.Application.Features.Items.Tasks;

public record GetDeletedTasksQuery;

public class GetDeletedTasksHandler(IItemRepository repository)
{
    public async Task<List<TaskDto>> HandleAsync(GetDeletedTasksQuery query, CancellationToken ct)
    {
        var tasks = await repository.GetDeletedTasksAsync(ct);
        return tasks.ToTaskExtensionDto();
    }
}
