using Misa.Application.Common.Abstractions.Persistence;

namespace Misa.Application.Scheduling.Commands.Deadlines;

public sealed class UpsertItemDeadlineHandler(IItemRepository items)
{
    public async Task Handle(UpsertItemDeadlineCommand command, CancellationToken ct = default)
    {
        await items.UpsertDeadlineAsync(command.ItemId, command.DueAtUtc, ct);
        await items.SaveChangesAsync(ct);
    }
}