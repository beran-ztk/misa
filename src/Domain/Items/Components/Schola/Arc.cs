namespace Misa.Domain.Items.Components.Schola;

public sealed class Arc
{
    private Arc() {} // EF
    public Arc(ItemId id)
    {
        Id = id;
    }
    public ItemId Id { get; init; }
}