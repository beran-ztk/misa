namespace Misa.Application.Features.Entities.Extensions.Items.Features.Deadlines.Events;

public class ItemDeadlineUpsertedEventHandler
{
    public static Task Handle(ItemDeadlineUpsertedEvent e, CancellationToken ct)
    {
        Console.WriteLine($"Deadline upserted: ItemId={e.ItemId}, DueAt={e.DueAtUtc}");
        return Task.CompletedTask;
    }
}