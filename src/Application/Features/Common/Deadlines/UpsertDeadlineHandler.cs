using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Common;

namespace Misa.Application.Features.Common.Deadlines;

public record UpsertDeadlineCommand(Guid ItemId, DateTimeOffset DueAtUtc);
public sealed class UpsertDeadlineHandler(IDeadlineRepository repository, ITimeProvider timeProvider)
{
    public async Task<Result> HandleAsync(UpsertDeadlineCommand command, CancellationToken ct)
    {
        var existingDeadline = await repository.TryGetDeadlineAsync(command.ItemId, ct);
        
        if (existingDeadline == null)
        {
            var deadline = new Deadline(command.ItemId, command.DueAtUtc, timeProvider.UtcNow);
            await repository.AddAsync(deadline, ct);
        }
        else
        {
            existingDeadline.SetDeadline(command.DueAtUtc);
        }

        await repository.SaveChangesAsync(ct);
        
        return Result.Ok();
    }
}