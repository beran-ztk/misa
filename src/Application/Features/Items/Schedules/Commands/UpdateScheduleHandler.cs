using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
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

public sealed class UpdateScheduleHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(UpdateScheduleCommand command)
    {
        var item = await repository.TryGetScheduleAsync(command.ItemId, CancellationToken.None);
        if (item?.ScheduleExtension is null)
            throw new DomainNotFoundException("schedule.not.found", command.ItemId.ToString());

        var nowUtc = timeProvider.UtcNow;

        if (!string.IsNullOrEmpty(command.Title))
            item.ChangeTitle(command.Title, nowUtc);
        if (command.Description is not null)
            item.ChangeDescription(command.Description, nowUtc);
        if (command.MisfirePolicy is { } policy)
            item.ChangeScheduleMisfirePolicy(policy.ToDomain(), nowUtc);
        if (command.LookaheadLimit is { } lookahead && lookahead > 0)
            item.ChangeScheduleLookaheadLimit(lookahead, nowUtc);

        // These fields carry the full intended state — null is a valid "clear" value
        item.ChangeScheduleOccurrenceCountLimit(command.OccurrenceCountLimit, nowUtc);
        item.ChangeScheduleStartTime(command.StartTime, nowUtc);
        item.ChangeScheduleEndTime(command.EndTime, nowUtc);
        item.ChangeScheduleActiveUntil(command.ActiveUntilUtc, nowUtc);

        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
