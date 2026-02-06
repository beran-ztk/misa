using Misa.Application.Abstractions.Persistence;
using Misa.Contract.Shared.Results;

namespace Misa.Application.Features.Entities.Features.Descriptions.Commands;

public record DeleteDescriptionCommand(Guid DescriptionId);

public sealed class DeleteDescriptionHandler(IEntityRepository repository)
{
    public async Task<Result> Handle(DeleteDescriptionCommand cmd, CancellationToken ct)
    {
        var description = await repository.GetDescriptionByIdAsync(cmd.DescriptionId, ct);
        if (description is null)
            return Result.NotFound("","Description not found");

        repository.RemoveDescription(description);
        await repository.SaveChangesAsync();

        return Result.Ok();
    }
}