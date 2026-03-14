using Misa.Domain.Items;

namespace Misa.Domain.Items.Components.Relations;

public sealed class ItemRelation
{
    private ItemRelation() { } // EF Core

    public ItemRelation(
        Guid id,
        Guid sourceItemId,
        Guid targetItemId,
        RelationType relationType,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        SourceItemId = new ItemId(sourceItemId);
        TargetItemId = new ItemId(targetItemId);
        RelationType = relationType;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; init; }
    public ItemId SourceItemId { get; init; }
    public ItemId TargetItemId { get; init; }
    public RelationType RelationType { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; init; }

    public void ChangeType(RelationType newType)
    {
        if (RelationType == newType) return;
        RelationType = newType;
    }

    // Navigation properties — loaded by EF when explicitly Included
    public Item? SourceItem { get; private set; }
    public Item? TargetItem { get; private set; }
}
