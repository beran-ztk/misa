using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Shared.Results;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schedules;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public record AddScheduleCommand(
    string Title,
    string Description,
    Guid? TargetItemId,
    ScheduleFrequencyTypeDto ScheduleFrequencyType,
    int FrequencyInterval,
    int LookaheadLimit,
    int? OccurrenceCountLimit,
    ScheduleMisfirePolicyDto MisfirePolicy,
    TimeSpan? OccurrenceTtl,
    ScheduleActionTypeDto ActionType,
    string? Payload,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DateTimeOffset ActiveFromLocal,
    DateTimeOffset? ActiveUntilLocal,
    int[]? ByDay,
    int[]? ByMonthDay,
    int[]? ByMonth
);

public class AddScheduleHandler(
    IItemRepository repository, 
    ITimeProvider timeProvider,
    ITimeZoneConverter timeZoneConverter, 
    IIdGenerator idGenerator,
    ICurrentUser currentUser)
{
    public async Task<Result<ScheduleExtensionDto>> Handle(AddScheduleCommand command, CancellationToken ct)
    {
        var scheduleExtension = new ScheduleExtension(
            targetItemId: command.TargetItemId,
            frequencyType: command.ScheduleFrequencyType.ToDomain(),
            frequencyInterval: command.FrequencyInterval,
            lookaheadLimit: command.LookaheadLimit,
            occurrenceCountLimit: command.OccurrenceCountLimit,
            misfirePolicy: command.MisfirePolicy.ToDomain(),
            occurrenceTtl: command.OccurrenceTtl,
            actionType: command.ActionType.ToDomain(),
            payload: command.Payload,
            startTime: command.StartTime,
            endTime: command.EndTime,
            activeFromUtc: timeZoneConverter.LocalToUtc(command.ActiveFromLocal, currentUser.Timezone),
            activeUntilUtc: timeZoneConverter.LocalToUtc(command.ActiveUntilLocal, currentUser.Timezone),
            byDay: command.ByDay,
            byMonthDay: command.ByMonthDay,
            byMonth: command.ByMonth,
            timezone: currentUser.Timezone
        );
            
        var scheduler = Item.CreateSchedule(
            id: new ItemId(idGenerator.New()), 
            ownerId: string.Empty,
            title: command.Title,
            description: command.Description,
            createdAtUtc: timeProvider.UtcNow,
            scheduleExtension: scheduleExtension
        );

        await repository.AddAsync(scheduler, ct);
        await repository.SaveChangesAsync(ct);

        return Result<ScheduleExtensionDto>.Ok(scheduler.ToScheduleExtensionDto());
    }
}