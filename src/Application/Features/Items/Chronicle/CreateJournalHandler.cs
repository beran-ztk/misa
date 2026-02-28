using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Items;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Chronicle.Journals;

namespace Misa.Application.Features.Items.Chronicle;

public sealed record CreateJournalCommand(
    string Title,
    string? Description,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset? UntilAtUtc
);

public sealed class CreateJournalHandler(
    IItemRepository repository,
    ITimeProvider timeProvider,
    IIdGenerator idGenerator,
    ICurrentUser currentUser)
{
    public async Task<ItemDto> Handle(CreateJournalCommand command, CancellationToken ct)
    {
        var journalExtension = new JournalExtension(
            occurredAt: command.OccurredAtUtc,
            untilAt: command.UntilAtUtc
        );

        var journalItem = Item.CreateJournal(
            id: new ItemId(idGenerator.New()),
            ownerId: currentUser.Id,
            title: command.Title,
            description: command.Description,
            createdAtUtc: timeProvider.UtcNow,
            journalExtension: journalExtension
        );

        await repository.AddAsync(journalItem, ct);
        await repository.SaveChangesAsync(ct);

        return journalItem.ToDto();
    }
}