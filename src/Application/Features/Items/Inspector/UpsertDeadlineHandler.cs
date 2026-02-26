using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Exceptions;

namespace Misa.Application.Features.Items.Inspector;

public record UpsertDeadlineCommand(Guid ItemId, DateTimeOffset? DueAtUtc);

public sealed class UpsertDeadlineHandler(IItemRepository repository)
{
    public async Task HandleAsync(UpsertDeadlineCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemDetailsAsync(command.ItemId, ct);
        if (item?.Activity is null)
            throw new DomainNotFoundException("item.not.found", "");

        item.Activity.SetDeadline(command.DueAtUtc);
        await repository.SaveChangesAsync(ct);
    }
}