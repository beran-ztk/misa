namespace Misa.Contract.Items.Components.Zettelkasten;

public record KnowledgeIndexEntryDto(Guid Id, WorkflowDto Workflow, string Title, Guid? ParentId, bool IsExpanded)
{
    public List<KnowledgeIndexEntryDto> Children { get; } = [];
}
