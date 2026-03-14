using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Schedules;
using Misa.Domain.Exceptions;

namespace Misa.Application.Features.Items.Schedules.Commands;

public record UpdateScheduleCommand(
    Guid ItemId,
    string? Title,
    string? Description,
    ScheduleMisfirePolicyDto? MisfirePolicy);

public sealed class UpdateScheduleHandler(IItemRepository repository)
{
    public async Task HandleAsync(UpdateScheduleCommand command)
    {
        var item = await repository.TryGetScheduleAsync(command.ItemId, CancellationToken.None);
        if (item?.ScheduleExtension is null)
            throw new DomainNotFoundException("schedule.not.found", command.ItemId.ToString());

        if (!string.IsNullOrEmpty(command.Title))
            item.ChangeTitle(command.Title);
        if (command.Description is not null)
            item.ChangeDescription(command.Description);
        if (command.MisfirePolicy is { } policy)
            item.ScheduleExtension.ChangeMisfirePolicy(policy.ToDomain());

        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
