using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Common.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public record AddScheduleCommand(
    string Title,
    ScheduleFrequencyTypeDto ScheduleFrequencyType,
    int FrequencyInterval,
    int LookaheadLimit,
    int? OccurrenceCountLimit,
    ScheduleMisfirePolicyDto MisfirePolicy,
    TimeSpan? OccurrenceTtl,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DateTimeOffset ActiveFromUtc,
    DateTimeOffset? ActiveUntilUtc
);

public class AddScheduleHandler(IItemRepository repository)
{
    public async Task<Result<ScheduleDto>> Handle(AddScheduleCommand command, CancellationToken ct)
    {
        try
        {
            var scheduler = Scheduler.Create(
                title: command.Title,
                frequencyType: command.ScheduleFrequencyType.MapToDomain(),
                frequencyInterval: command.FrequencyInterval,
                lookaheadLimit: command.LookaheadLimit,
                occurrenceCountLimit: command.OccurrenceCountLimit,
                misfirePolicy: command.MisfirePolicy.MapToDomain(),
                occurrenceTtl: command.OccurrenceTtl,
                startTime: command.StartTime,
                endTime: command.EndTime,
                activeFromUtc: command.ActiveFromUtc,
                activeUntilUtc: command.ActiveUntilUtc
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