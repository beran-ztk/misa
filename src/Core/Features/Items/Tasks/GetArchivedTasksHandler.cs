using Misa.Contract.Items.Components.Tasks;
using Misa.Core.Abstractions.Persistence;
using Misa.Core.Mappings;

namespace Misa.Core.Features.Items.Tasks;

public record GetArchivedTasksQuery;

public class GetArchivedTasksHandler(IItemRepository repository)
{
    public async Task<List<TaskDto>> HandleAsync(GetArchivedTasksQuery query, CancellationToken ct)
    {
        var tasks = await repository.GetArchivedTasksAsync(ct);
        return tasks.ToTaskExtensionDto();
    }
}
