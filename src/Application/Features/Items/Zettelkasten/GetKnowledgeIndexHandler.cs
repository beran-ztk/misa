using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Zettelkasten;

namespace Misa.Application.Features.Items.Zettelkasten;

public sealed record GetKnowledgeIndexQuery;

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
                item.Workflow.ToWorkflowDto(),
                item.Title,
                item.KnowledgeIndex!.ParentId?.Value,
                item.KnowledgeIndex!.IsExpanded
            ))
        );

        var rootEntries = Entries.Where(e => e.ParentId is null).ToList();
        var sortedEntries = new List<KnowledgeIndexEntryDto>(rootEntries.Count);
        foreach (var entry in rootEntries)
        {
            AppendChildren(entry);
            
            sortedEntries.Add(entry);
        }
        
        return sortedEntries;
    }

    private void AppendChildren(KnowledgeIndexEntryDto currentEntry)
    {
        foreach (var entry in Entries)
        {
            // Does any entry have currentEntry as Parent?
            if (entry.ParentId == currentEntry.Id)
            {
                AppendChildren(entry);
                
                currentEntry.Children.Add(entry);
            }
        }
    }
}