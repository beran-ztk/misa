using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Features.Common.Deadlines;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Common;

namespace Misa.Application.Features.Common.Deadlines;

public record UpsertDeadlineCommand(Guid ItemId, DateTimeOffset DueAtUtc);
public sealed class UpsertDeadlineHandler(IDeadlineRepository repository, ITimeProvider timeProvider)
{
    public async Task<Result<DeadlineDto>> HandleAsync(UpsertDeadlineCommand command, CancellationToken ct)
    {
        var existingDeadline = await repository.TryGetDeadlineAsync(command.ItemId, ct);
        
        if (existingDeadline == null)
        {
            existingDeadline = new Deadline(command.ItemId, command.DueAtUtc, timeProvider.UtcNow);
            await repository.AddAsync(existingDeadline, ct);
        }
        else
        {
            existingDeadline.SetDeadline(command.DueAtUtc);
        }

        await repository.SaveChangesAsync(ct);
        
        return Result<DeadlineDto>.Ok(existingDeadline.ToDto());
    }
}