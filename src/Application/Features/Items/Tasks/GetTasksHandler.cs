using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Tasks;

// Request
public record GetTasksQuery;

// Handler
public class GetTasksHandler(ItemRepository repository)
{
    // Handle
    public async Task<List<Item>> HandleAsync(GetTasksQuery query, CancellationToken ct)
    {
        var tasks = await repository.GetTasksAsync(ct);
        return tasks;
    }
}