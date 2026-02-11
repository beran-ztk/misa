namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;

public sealed record UpsertDeadlineDto(Guid ItemId, DateTimeOffset DueAtUtc);
