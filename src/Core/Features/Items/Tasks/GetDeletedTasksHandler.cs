using Misa.Contract.Items.Components.Tasks;
using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Mappings;

namespace Misa.Core.Features.Items.Tasks;

public record GetDeletedTasksQuery;

public class GetDeletedTasksHandler(IItemRepository repository)
{
    public async Task<List<TaskDto>> HandleAsync(GetDeletedTasksQuery query, CancellationToken ct)
    {
        var tasks = await repository.GetDeletedTasksAsync(ct);
        return tasks.ToTaskExtensionDto();
    }
}
