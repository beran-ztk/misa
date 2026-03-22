namespace Misa.Contract.Items.Components.Zettelkasten;

public record DeletedKnowledgeEntryDto(
    Guid         Id,
    WorkflowDto  Workflow,
    string       Title,
    Guid?        ParentId,
    DateTimeOffset? DeletedAt);
