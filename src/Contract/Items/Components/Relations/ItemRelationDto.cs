namespace Misa.Contract.Items.Components.Relations;

public sealed record ItemRelationDto(
    Guid RelationId,
    string RelationType,
    Guid SourceItemId,
    string SourceItemTitle,
    Guid TargetItemId,
    string TargetItemTitle
);
