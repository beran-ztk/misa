using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Items.Components.Schedules;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public record AddScheduleCommand(
    Guid UserId,
    string Title,
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
    IAuthenticationRepository authenticationRepository,
    ITimeProvider timeProvider,
    ITimeZoneConverter timeZoneConverter, 
    IIdGenerator idGenerator)
{
    public async Task<Result<ScheduleDto>> Handle(AddScheduleCommand command, CancellationToken ct)
    {
        var user = await authenticationRepository.FindByIdAsync(command.UserId, ct);
            
        var scheduler = ScheduleExtension.Create(
            ownerId: string.Empty,
            id: idGenerator.New(), 
            title: command.Title,
            targetItemId: command.TargetItemId,
            frequencyType: command.ScheduleFrequencyType.MapToDomain(),
            frequencyInterval: command.FrequencyInterval,
            lookaheadLimit: command.LookaheadLimit,
            occurrenceCountLimit: command.OccurrenceCountLimit,
            misfirePolicy: command.MisfirePolicy.MapToDomain(),
            occurrenceTtl: command.OccurrenceTtl,
            actionType: command.ActionType.ToDomain(),
            payload: command.Payload,
            startTime: command.StartTime,
            endTime: command.EndTime,
            activeFromUtc: timeZoneConverter.LocalToUtc(command.ActiveFromLocal, user.TimeZone),
            activeUntilUtc: timeZoneConverter.LocalToUtc(command.ActiveUntilLocal, user.TimeZone),
            byDay: command.ByDay,
            byMonthDay: command.ByMonthDay,
            byMonth: command.ByMonth,
            timezone: user.TimeZone,
            createdAt: timeProvider.UtcNow
        );

        await repository.AddAsync(scheduler, ct);
        await repository.SaveChangesAsync(ct);

        return Result<ScheduleDto>.Ok(scheduler.ToDto());
    }
}