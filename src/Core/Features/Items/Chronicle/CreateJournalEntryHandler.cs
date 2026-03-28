using Misa.Core.Common.Abstractions.Ids;
using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Common.Abstractions.Time;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Chronicle.Journals;

namespace Misa.Core.Features.Items.Chronicle;

public sealed record CreateJournalCommand(
    string Title,
    string? Description,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset? UntilAtUtc
);

public sealed class CreateJournalEntryHandler(
    IItemRepository repository,
    ITimeProvider timeProvider,
    IIdGenerator idGenerator)
{
    public async Task<Item> Handle(CreateJournalCommand command, CancellationToken ct)
    {
        var journalExtension = new JournalExtension(
            occurredAt: command.OccurredAtUtc,
            untilAt: command.UntilAtUtc
        );

        var journalItem = Item.CreateJournal(
            id: new ItemId(idGenerator.New()),
            title: command.Title,
            description: command.Description,
            createdAtUtc: timeProvider.UtcNow,
            journalExtension: journalExtension
        );

        await repository.AddAsync(journalItem, ct);
        await repository.SaveChangesAsync(ct);

        return journalItem;
    }
}