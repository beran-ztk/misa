using Misa.Application.Items.Repositories;

namespace Misa.Application.Scheduling.Commands.UpsertItemDeadline;

public sealed class UpsertItemDeadlineHandler(IItemRepository items)
{
    public async Task Handle(UpsertItemDeadlineCommand command, CancellationToken ct = default)
    {
        await items.UpsertDeadlineAsync(command.ItemId, command.DueAtUtc, ct);
        await items.SaveChangesAsync(ct);
    }
}