namespace Misa.Domain.Features.Messaging;

public class Outbox
{
    private Outbox() { }

    public Outbox(EventType eventType, string payload, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(payload)) throw new ArgumentException("Payload must not be empty.", nameof(payload));

        EventType = eventType;
        Payload = payload;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid EventId { get; private set; }

    public EventType EventType { get; private set; }
    public OutboxEventState EventState { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    
    public DateTimeOffset CreatedAtUtc { get; private set; }
}