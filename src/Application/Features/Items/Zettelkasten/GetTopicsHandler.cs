using Misa.Application.Abstractions.Persistence;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Zettelkasten;

namespace Misa.Application.Features.Items.Zettelkasten;

public sealed record GetKnowledgeIndexQuery;

public sealed class GetKnowledgeIndexHandler(IItemRepository repository)
{
    public async Task<List<KnowledgeIndexEntryDto>> HandleAsync(GetKnowledgeIndexQuery query, CancellationToken ct)
    {
        var topicItems = await repository.GetKnowledgeIndexAsync();
        var zettelItems = await repository.GetZettelsAsync(null, ct);

        var entries = new List<KnowledgeIndexEntryDto>(topicItems.Count + zettelItems.Count);

        foreach (var item in topicItems)
            entries.Add(new KnowledgeIndexEntryDto(item.Id.Value, item.Title, item.KnowledgeIndex?.ParentId?.Value, WorkflowDto.Topic));

        foreach (var item in zettelItems)
            entries.Add(new KnowledgeIndexEntryDto(item.Id.Value, item.Title, item.KnowledgeIndex?.ParentId?.Value, WorkflowDto.Zettel));

        return entries;
    }
}
