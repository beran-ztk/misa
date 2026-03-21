using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Items.Components.Audits.Changes;

namespace Misa.Domain.Items.Components.Zettelkasten;

public sealed class Zettel : DomainEventEntity
{
    private Zettel() { } // EF

    public Zettel(ItemId id)
    {
        Id = id;
    }

    public ItemId Id { get; init; }
    public string? Content { get; private set; }

    public void ChangeContent(string? content)
    {
        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.Content, Content, content, null));
        Content = content;
    }
}
