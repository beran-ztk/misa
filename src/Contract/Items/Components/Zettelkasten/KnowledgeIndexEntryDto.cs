using Misa.Contract.Items;

namespace Misa.Contract.Items.Components.Zettelkasten;

public record KnowledgeIndexEntryDto(Guid Id, string Title, Guid? ParentId, WorkflowDto Workflow)
{
    public List<KnowledgeIndexEntryDto> Children { get; } = [];
}
