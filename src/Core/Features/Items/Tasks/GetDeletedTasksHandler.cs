using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Tasks;

public record GetDeletedTasksQuery;

public class GetDeletedTasksHandler(IItemRepository repository)
{
    public async Task<List<Item>> HandleAsync(GetDeletedTasksQuery query, CancellationToken ct)
    {
        var tasks = await repository.GetDeletedTasksAsync(ct);
        return tasks;
    }
}
