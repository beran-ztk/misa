using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Common.Abstractions.Time;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Chronicle;

public sealed record UpdateJournalCommand(
    Guid           ItemId,
    string?        Description,
    DateTimeOffset OccurredAtUtc);

public sealed class UpdateJournalHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(UpdateJournalCommand command)
    {
        var item = await repository.TryGetJournalAsync(command.ItemId);
        if (item is null)
            throw new DomainNotFoundException("Item not found", "");

        var nowUtc = timeProvider.UtcNow;

        item.ChangeDescription(command.Description ?? string.Empty, nowUtc);
        item.ChangeJournalOccurredAt(command.OccurredAtUtc, nowUtc);

        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
