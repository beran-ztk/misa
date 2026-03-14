using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Schedules;
using Misa.Domain.Exceptions;

namespace Misa.Application.Features.Items.Schedules.Commands;

public record UpdateScheduleCommand(
    Guid ItemId,
    string? Title,
    string? Description,
    ScheduleMisfirePolicyDto? MisfirePolicy,
    int? LookaheadLimit,
    int? OccurrenceCountLimit,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DateTimeOffset? ActiveUntilUtc);

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
        if (command.LookaheadLimit is { } lookahead && lookahead > 0)
            item.ScheduleExtension.ChangeLookaheadLimit(lookahead);

        // These fields carry the full intended state — null is a valid "clear" value
        item.ScheduleExtension.ChangeOccurrenceCountLimit(command.OccurrenceCountLimit);
        item.ScheduleExtension.ChangeStartTime(command.StartTime);
        item.ScheduleExtension.ChangeEndTime(command.EndTime);
        item.ScheduleExtension.ChangeActiveUntil(command.ActiveUntilUtc);

        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
