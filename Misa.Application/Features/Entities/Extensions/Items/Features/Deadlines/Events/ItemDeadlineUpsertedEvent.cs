namespace Misa.Application.Features.Entities.Extensions.Items.Features.Deadlines.Events;

public record ItemDeadlineUpsertedEvent(Guid ItemId, DateTimeOffset DueAtUtc);