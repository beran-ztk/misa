using Misa.Core.Persistence;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Journal;

namespace Misa.Core.Features.Items.Chronicle;

public sealed record CreateJournalCommand(
    string Title,
    string? Description,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset? UntilAtUtc
);

public sealed class CreateJournalEntryHandler(ItemRepository repository)
{
    public async Task<Item> Handle(CreateJournalCommand command, CancellationToken ct)
    {
        var journalExtension = new JournalExtension(
            occurredAt: command.OccurredAtUtc,
            untilAt: command.UntilAtUtc
        );

        var journalItem = Item.CreateJournal(
            title: command.Title,
            description: command.Description,
            journalExtension: journalExtension
        );

        await repository.AddAsync(journalItem, ct);
        await repository.SaveChangesAsync(ct);

        return journalItem;
    }
}