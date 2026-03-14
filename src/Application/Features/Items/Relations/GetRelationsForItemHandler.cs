using Misa.Application.Abstractions.Persistence;
using Misa.Contract.Items.Components.Relations;

namespace Misa.Application.Features.Items.Relations;

public sealed record GetRelationsForItemQuery(Guid ItemId);

public sealed class GetRelationsForItemHandler(IItemRepository repository)
{
    public async Task<List<ItemRelationDto>> HandleAsync(GetRelationsForItemQuery query, CancellationToken ct)
    {
        var relations = await repository.GetRelationsForItemAsync(query.ItemId, ct);

        return relations.Select(r => new ItemRelationDto(
            r.Id,
            r.RelationType.ToString(),
            r.SourceItemId.Value,
            r.SourceItem?.Title ?? string.Empty,
            r.TargetItemId.Value,
            r.TargetItem?.Title ?? string.Empty
        )).ToList();
    }
}
