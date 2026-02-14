using Misa.Application.Abstractions.Persistence;
using Misa.Contract.Shared.Results;

namespace Misa.Application.Features.Common.Deadlines;
public record DeleteDeadlineCommand(Guid ItemId);
public sealed class DeleteDeadlineHandler(IDeadlineRepository repository)
{
    public async Task<Result> HandleAsync(DeleteDeadlineCommand command, CancellationToken ct)
    {
        var existingDeadline = await repository.TryGetDeadlineAsync(command.ItemId, ct);

        if (existingDeadline == null) 
            return Result.Ok();
        
        await repository.Remove(existingDeadline);
        await repository.SaveChangesAsync(ct);
        
        return Result.Ok();
    }
}