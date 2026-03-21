namespace Misa.Domain.Items.Components.Zettelkasten;

public class Topic
{
    private Topic() {} // EF

    public Topic(ItemId id)
    {
        Id = id;
    }

    public ItemId Id { get; init; }
    public string? Content { get; private set; }
}
