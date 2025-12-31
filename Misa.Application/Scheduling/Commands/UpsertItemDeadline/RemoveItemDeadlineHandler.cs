using Misa.Application.Items.Repositories;

namespace Misa.Application.Scheduling.Commands.UpsertItemDeadline;

public sealed class RemoveItemDeadlineHandler(IItemRepository items)
{
    public async Task Handle(RemoveItemDeadlineCommand command, CancellationToken ct = default)
    {
        await items.RemoveDeadlineAsync(command.ItemId, ct);
        await items.SaveChangesAsync(ct);
    }
}