using Misa.Application.Common.Abstractions.Events;
using Misa.Application.Scheduling.Events.Commands;
using Misa.Contract.Events;

namespace Misa.Api.Endpoints.Scheduling;

public class ItemDeadlineRemoveEventHandler(IEventPublisher events)
{
    public async Task Handle(ItemDeadlineRemovedEvent e, CancellationToken ct)
    {
        Console.WriteLine($"Deadline removed: ItemId={e.ItemId}");
        
        var eventDto = new EventDto
        {
            EventType = "DeadlineRemoved",
            Payload = System.Text.Json.JsonSerializer.Serialize(new { e.ItemId }),
            TimestampUtc = DateTimeOffset.UtcNow
        };
        await events.PublishAsync(eventDto, ct);
    }
}