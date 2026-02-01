using Misa.Application.Common.Abstractions.Persistence;
using Misa.Contract.Common.Results;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public sealed record DeleteDeadlineCommand(Guid TargetItemId);

public sealed class DeleteDeadlineHandler(ISchedulerRepository repository)
{
    public async Task<Result> HandleAsync(DeleteDeadlineCommand command, CancellationToken ct)
    {
        var deadline = await repository.TryGetDeadlineFromTargetItemIdAsync(command.TargetItemId, ct);
        if (deadline is null)
            return Result.Ok();

        repository.Remove(deadline);
        await repository.SaveChangesAsync(ct);

        return Result.Ok();
    }
}