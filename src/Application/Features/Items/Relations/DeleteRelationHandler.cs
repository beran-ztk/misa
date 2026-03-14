using Misa.Application.Abstractions.Persistence;

namespace Misa.Application.Features.Items.Relations;

public sealed record DeleteRelationCommand(Guid RelationId);

public sealed class DeleteRelationHandler(IItemRepository repository)
{
    public async Task HandleAsync(DeleteRelationCommand command, CancellationToken ct)
    {
        await repository.DeleteRelationAsync(command.RelationId, ct);
        await repository.SaveChangesAsync(ct);
    }
}
