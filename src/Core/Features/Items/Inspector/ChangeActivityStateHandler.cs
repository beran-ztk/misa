using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Exceptions;
using Misa.Domain.Items.Components.Activities;

namespace Misa.Core.Features.Items.Inspector;

public record ChangeActivityStateCommand(Guid ItemId, ActivityState State, string? Reason = null);

public sealed class ChangeActivityStateHandler(ItemRepository repository)
{
    public async Task HandleAsync(ChangeActivityStateCommand command, CancellationToken ct)
    {
        if (command.State == ActivityState.Expired)
            throw new DomainValidationException("State", "activity.state.expired.not.allowed", "Expired is a system-only state and cannot be set manually.");

        var item = await repository.TryGetItemDetailsAsync(command.ItemId, ct);
        if (item?.Activity is null)
            throw new DomainNotFoundException("item.not.found", command.ItemId.ToString());

        item.Activity.ChangeState(command.State, DateTimeOffset.UtcNow, command.Reason);

        await repository.SaveChangesAsync(ct);
    }
}
