using System;
using System.Threading;
using System.Threading.Tasks;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Features.Chronicle;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Audit;

namespace Misa.Application.Features.Chronicle;

// Command
public sealed record CreateJournalEntryCommand(
    Guid UserId,
    string Description,
    DateTimeOffset OccurredAt,
    DateTimeOffset? UntilAt,
    Guid? CategoryId
);

// Handler
public sealed class CreateJournalEntryHandler(
    IChronicleRepository repository,
    ITimeProvider timeProvider,
    IIdGenerator idGenerator)
{
    public async Task<Result<JournalEntryDto>> HandleAsync(CreateJournalEntryCommand command, CancellationToken ct)
    {
        var journal = await repository.GetJournalAsync(command.UserId, ct);
        var id = idGenerator.New();
        var createdAt = timeProvider.UtcNow;

        var entry = new JournalEntry(
            id: new JournalEntryId(id),
            journalId: journal.Id,
            description: command.Description.Trim(),
            occurredAt: command.OccurredAt.ToUniversalTime(),
            untilAt: command.UntilAt?.ToUniversalTime(),
            createdAt: createdAt,
            originId: null,
            systemType: null,
            categoryId: command.CategoryId is null
                ? null
                : new JournalCategoryId(command.CategoryId.Value)
        );

        await repository.AddAsync(entry, ct);
        await repository.SaveChangesAsync(ct);
        
        return Result<JournalEntryDto>.Ok(entry.ToDto());
    }
}