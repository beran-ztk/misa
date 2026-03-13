namespace Misa.Domain.Items.Components.Zettelkasten;

public sealed class ZettelExtension
{
    private ZettelExtension() { } // EF

    public ZettelExtension(ItemId id, ItemId topicId, string? content)
    {
        Id = id;
        TopicId = topicId;
        Content = content;
    }

    public ItemId Id { get; init; }
    public ItemId TopicId { get; private set; }
    public string? Content { get; private set; }

    public void ChangeContent(string? content) => Content = content;
}
