using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Zettelkasten;

namespace Misa.Application.Features.Items.Zettelkasten;

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
