namespace Misa.Contract.Items.Components.Relations;

public sealed record CreateRelationRequest(
    Guid SourceItemId,
    Guid TargetItemId,
    RelationTypeDto RelationType
);
