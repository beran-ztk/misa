using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Common.Abstractions.Time;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Inspector;

public record UpsertDeadlineCommand(Guid ItemId, DateTimeOffset? DueAtUtc);

public sealed class UpsertDeadlineHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(UpsertDeadlineCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemDetailsAsync(command.ItemId, ct);
        if (item?.Activity is null)
            throw new DomainNotFoundException("item.not.found", "");

        item.Activity.SetDeadline(command.DueAtUtc, timeProvider.UtcNow);
        await repository.SaveChangesAsync(ct);
    }
}
