using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record GetKnowledgeIndexQuery;

public record KnowledgeIndexEntryDto(
    Guid Id,
    Workflow Workflow,
    string Title,
    Guid? ParentId,
    bool IsExpanded)
{
    public List<KnowledgeIndexEntryDto> Children { get; set; } = [];
}
public sealed class GetKnowledgeIndexHandler(IItemRepository repository)
{
    private List<KnowledgeIndexEntryDto> Entries { get; set; } = [];
    public async Task<List<KnowledgeIndexEntryDto>> HandleAsync(GetKnowledgeIndexQuery query, CancellationToken ct)
    {
        var knowledgeIndexEntries = await repository.GetKnowledgeIndexAsync();

        Entries = new List<KnowledgeIndexEntryDto>(knowledgeIndexEntries.Count);

        Entries.AddRange(knowledgeIndexEntries
            .Where(item => item.KnowledgeIndex is not null)
            .Select(item => new KnowledgeIndexEntryDto(
                item.Id.Value,
                item.Workflow,
                item.Title,
                item.KnowledgeIndex!.ParentId?.Value,
                item.KnowledgeIndex!.IsExpanded
            ))
        );

        var allIds = Entries.Select(e => e.Id).ToHashSet();

        var rootEntries = Entries
            .Where(e => e.ParentId is null || !allIds.Contains(e.ParentId.Value))
            .OrderBy(e => e.Title)
            .ToList();
        
        var sortedEntries = new List<KnowledgeIndexEntryDto>(rootEntries.Count);
        foreach (var entry in rootEntries)
        {
            AppendChildren(entry);
            
            sortedEntries.Add(entry);
        }

        sortedEntries = sortedEntries
            .OrderBy(e => e.Workflow switch
            {
                Workflow.Topic => 0,
                Workflow.Zettel => 1,
                _ => 99
            })
            .ThenBy(e => e.Title)
            .ToList();
        
        return sortedEntries;
    }

    private void AppendChildren(KnowledgeIndexEntryDto currentEntry)
    {
        foreach (var entry in Entries.Where(entry => entry.ParentId == currentEntry.Id))
        {
            AppendChildren(entry);
                
            currentEntry.Children.Add(entry);
        }
    }
}