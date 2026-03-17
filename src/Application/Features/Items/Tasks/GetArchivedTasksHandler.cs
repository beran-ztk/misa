using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Tasks;

namespace Misa.Application.Features.Items.Tasks;

public record GetArchivedTasksQuery;

public class GetArchivedTasksHandler(IItemRepository repository)
{
    public async Task<List<TaskDto>> HandleAsync(GetArchivedTasksQuery query, CancellationToken ct)
    {
        var tasks = await repository.GetArchivedTasksAsync(ct);
        return tasks.ToTaskExtensionDto();
    }
}
