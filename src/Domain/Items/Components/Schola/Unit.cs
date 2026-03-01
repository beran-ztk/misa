using Misa.Domain.Items.Components.Activities;

namespace Misa.Domain.Items.Components.Schola;

public sealed class Unit
{
    private Unit() {} // EF
    public Unit(ItemId id, ItemId? arcId)
    {
        Id = id;
        ArcId = arcId;
    }
    public ItemId Id { get; init; }
    public ItemId? ArcId { get; init; }

    public Item Item { get; private set; } = null!;
}