using Misa.Application.Common.Abstractions.Persistence;
using Misa.Contract.Common.Results;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public sealed record CreateOnceScheduleCommand(
    Guid TargetItemId,
    DateTimeOffset DueAtUtc
);

public sealed class CreateOnceScheduleHandler(ISchedulerRepository repository)
{
    public async Task<Result> HandleAsync(CreateOnceScheduleCommand command, CancellationToken ct)
    {
        if (await repository.ExistsOnceForTargetAsync(command.TargetItemId, ct))
            return Result.Invalid("","Once-Scheduler für dieses Item existiert bereits.");
        
        var scheduler = Scheduler.CreateOnce(
            targetItemId: command.TargetItemId,
            dueAtUtc: command.DueAtUtc
        );

        await repository.AddAsync(scheduler, ct);
        await repository.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
