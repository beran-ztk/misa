using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record GetDeletedKnowledgeIndexQuery;
public record DeletedKnowledgeEntryDto(
    Guid         Id,
    Workflow  Workflow,
    string       Title,
    Guid?        ParentId,
    DateTimeOffset? DeletedAt);
public sealed class GetDeletedKnowledgeIndexHandler(ItemRepository repository)
{
    public async Task<List<DeletedKnowledgeEntryDto>> HandleAsync(
        GetDeletedKnowledgeIndexQuery query, CancellationToken ct)
    {
        var items = await repository.GetDeletedKnowledgeIndexAsync();

        return items
            .Select(item => new DeletedKnowledgeEntryDto(
                item.Id.Value,
                item.Workflow,
                item.Title,
                item.KnowledgeIndex?.ParentId?.Value,
                item.ModifiedAt))
            .OrderByDescending(e => e.DeletedAt)
            .ToList();
    }
}
