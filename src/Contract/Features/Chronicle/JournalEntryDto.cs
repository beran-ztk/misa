namespace Misa.Contract.Features.Chronicle;

public sealed record JournalEntryDto(
    Guid Id,
    Guid JournalId,

    string Description,

    DateTimeOffset OccurredAt,
    DateTimeOffset? UntilAt,

    DateTimeOffset CreatedAt,

    Guid? OriginId,
    JournalSystemTypeDto? SystemType,

    Guid? CategoryId
);