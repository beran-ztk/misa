using Misa.Contract.Items;

namespace Misa.Contract.Items.Components.Relations;

public sealed record ItemRelationDto(
    Guid RelationId,
    RelationTypeDto RelationType,
    Guid SourceItemId,
    string SourceItemTitle,
    WorkflowDto SourceItemWorkflow,
    Guid TargetItemId,
    string TargetItemTitle,
    WorkflowDto TargetItemWorkflow
);
