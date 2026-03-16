using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Activity;
using Misa.Domain.Exceptions;

namespace Misa.Application.Features.Items.Inspector;

public record ChangeActivityStateCommand(
    Guid ItemId,
    ActivityStateDto State,
    string? Reason = null);

public sealed class ChangeActivityStateHandler(IItemRepository repository)
{
    public async Task HandleAsync(ChangeActivityStateCommand command, CancellationToken ct)
    {
        if (command.State == ActivityStateDto.Expired)
            throw new DomainValidationException("State", "activity.state.expired.not.allowed", "Expired is a system-only state and cannot be set manually.");

        var item = await repository.TryGetItemDetailsAsync(command.ItemId, ct);
        if (item?.Activity is null)
            throw new DomainNotFoundException("item.not.found", command.ItemId.ToString());

        item.Activity.ChangeState(command.State.ToDomain(), command.Reason);

        await repository.SaveChangesAsync(ct);
    }
}
