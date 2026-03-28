using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Items.Components.Relations;

namespace Misa.Core.Features.Items.Relations;

public sealed record GetRelationsForItemQuery(Guid ItemId);
public sealed class GetRelationsForItemHandler(IItemRepository repository)
{
    public async Task<List<ItemRelation>> HandleAsync(GetRelationsForItemQuery query, CancellationToken ct)
    {
        var relations = await repository.GetRelationsForItemAsync(query.ItemId, ct);

        return relations;
    }
}
