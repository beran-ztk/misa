using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Items.Components.Audits.Changes;

namespace Misa.Domain.Items.Components.Zettelkasten;

public sealed class ZettelExtension : DomainEventEntity
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

    public void ChangeContent(string? content)
    {
        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.Content, Content, content, null));
        Content = content;
    }
}
