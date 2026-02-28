namespace Misa.Contract.Items.Components.Chronicle;

public sealed record JournalExtensionDto(
    Guid ItemId,
    DateTimeOffset OccurredAt,
    DateTimeOffset? UntilAt
);