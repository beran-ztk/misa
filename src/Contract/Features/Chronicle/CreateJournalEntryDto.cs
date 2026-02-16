namespace Misa.Contract.Features.Chronicle;

public sealed record CreateJournalEntryDto(
    string Description,
    DateTimeOffset OccurredAt,
    DateTimeOffset? UntilAt,
    Guid? CategoryId
);