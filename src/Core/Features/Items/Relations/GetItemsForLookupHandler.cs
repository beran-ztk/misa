using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Relations;

public sealed record GetItemsForLookupQuery;

public sealed record ItemLookupDto(Guid Id, string Title, Workflow Workflow);
public sealed class GetItemsForLookupHandler(IItemRepository repository)
{
    public async Task<List<ItemLookupDto>> HandleAsync(GetItemsForLookupQuery query, CancellationToken ct)
    {
        var items = await repository.GetItemsForLookupAsync(ct);

        return items.Select(i => new ItemLookupDto(
            i.Id.Value,
            i.Title,
            i.Workflow
        )).ToList();
    }
}
