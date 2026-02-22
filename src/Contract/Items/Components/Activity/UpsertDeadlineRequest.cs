namespace Misa.Contract.Items.Components.Activity;

public sealed record UpsertDeadlineRequest(DateTimeOffset? DueAtUtc);
