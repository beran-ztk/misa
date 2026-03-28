using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Mappings;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record GetDeletedKnowledgeIndexQuery;

public sealed class GetDeletedKnowledgeIndexHandler(IItemRepository repository)
{
    public async Task<List<DeletedKnowledgeEntryDto>> HandleAsync(
        GetDeletedKnowledgeIndexQuery query, CancellationToken ct)
    {
        var items = await repository.GetDeletedKnowledgeIndexAsync();

        return items
            .Select(item => new DeletedKnowledgeEntryDto(
                item.Id.Value,
                item.Workflow.ToWorkflowDto(),
                item.Title,
                item.KnowledgeIndex?.ParentId?.Value,
                item.ModifiedAt))
            .OrderByDescending(e => e.DeletedAt)
            .ToList();
    }
}
