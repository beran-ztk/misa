using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Common.Abstractions.Time;
using Misa.Application.Common.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

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

public class AddScheduleHandler(IItemRepository repository, IAuthenticationRepository authenticationRepository)
{
    public async Task<Result<ScheduleDto>> Handle(AddScheduleCommand command, CancellationToken ct)
    {
        try
        {
            var user = await authenticationRepository.FindByIdAsync(command.UserId, ct);
            if (user == null)
                return Result<ScheduleDto>.Invalid("", "No user found");
            
            var scheduler = Scheduler.Create(
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
                activeFromUtc: command.ActiveFromLocal.LocalToUtc(user.TimeZone),
                activeUntilUtc: command.ActiveUntilLocal.LocalToUtc(user.TimeZone),
                byDay: command.ByDay,
                byMonthDay: command.ByMonthDay,
                byMonth: command.ByMonth,
                timezone: user.TimeZone
            );

            await repository.AddAsync(scheduler, ct);
            await repository.SaveChangesAsync(ct);

            return Result<ScheduleDto>.Ok(scheduler.ToDto());
        }
        catch (ArgumentException ex)
        {
            return Result<ScheduleDto>.Invalid("", ex.Message);
        }
    }
}