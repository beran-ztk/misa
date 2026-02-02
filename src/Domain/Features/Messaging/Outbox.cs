namespace Misa.Domain.Features.Messaging;

public class Outbox
{
    private Outbox() { }

    public Outbox(Guid eventId, EventType eventType, string payload, DateTime createdAtUtc)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("EventId must not be empty.", nameof(eventId));
        if (string.IsNullOrWhiteSpace(payload)) throw new ArgumentException("Payload must not be empty.", nameof(payload));

        EventId = eventId;
        EventType = eventType;
        Payload = payload;
        CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
    }

    public Guid EventId { get; private set; }

    public EventType EventType { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    
    public DateTime CreatedAtUtc { get; private set; }
}