using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Tasks;

public record GetArchivedTasksQuery;

public class GetArchivedTasksHandler(ItemRepository repository)
{
    public async Task<List<Item>> HandleAsync(GetArchivedTasksQuery query, CancellationToken ct)
    {
        var tasks = await repository.GetArchivedTasksAsync(ct);
        return tasks;
    }
}
