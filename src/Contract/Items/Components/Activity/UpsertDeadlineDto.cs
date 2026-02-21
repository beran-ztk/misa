namespace Misa.Contract.Items.Components.Activity;

public sealed record UpsertDeadlineDto(Guid ItemId, DateTimeOffset DueAtUtc);
