using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Contract.Common.Results;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public sealed record CreateOnceScheduleCommand(
    Guid TargetItemId,
    DateTimeOffset DueAtUtc
);

public sealed class CreateOnceScheduleHandler(ISchedulerRepository repository, ITimeProvider timeProvider)
{
    public async Task<Result> HandleAsync(CreateOnceScheduleCommand command, CancellationToken ct)
    {
        var deadline = await repository.TryGetDeadlineFromTargetItemIdAsync(command.TargetItemId, ct);
        if (deadline is not null)
        {
            deadline.ActiveFromUtc = command.DueAtUtc;
        }
        else
        {
            var scheduler = Scheduler.CreateOnce(
                targetItemId: command.TargetItemId,
                dueAtUtc: command.DueAtUtc,
                createdAtUtc: timeProvider.UtcNow
            );
            
            await repository.AddAsync(scheduler, ct);
        }
        
        await repository.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
